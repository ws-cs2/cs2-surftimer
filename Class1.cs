using System.Drawing;
using System.Text.Json;
using System.Text.RegularExpressions;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using Newtonsoft.Json;
using Supabase.Realtime;
using Supabase.Realtime.Models;
using JsonSerializer = System.Text.Json.JsonSerializer;
using Timer = System.Threading.Timer;

namespace WST;

public class OnStartTouchEvent
{
    public string TriggerName { get; set; }
    public int PlayerSlot { get; set; }
}

public class OnEndTouchEvent
{
    public string TriggerName { get; set; }
    public int PlayerSlot { get; set; }
}

public class OnTickEvent
{
}

public class ServerConfig
{
    public string Id { get; set; }
}

public class ChatBroadcast : BaseBroadcast
{
    [JsonProperty("message")]
    public string Message { get; set; }
}


public partial class Wst : BasePlugin
{
    public Wst()
    {
    }

    public override string ModuleName => "WST";
    public override string ModuleVersion => "2.0.0";
    
    public static string SupabaseUrl = Environment.GetEnvironmentVariable("SUPABASE_URL")!;
    public static string SupabaseToken = Environment.GetEnvironmentVariable("SUPABASE_TOKEN")!;

    private EntityManager _entityManager;
    private EventManager _eventManager;
    private Database _database;
    
    private List<string> _dynamicCommands = new List<string>();
    
    private List<WorkshopMapInfo> _workshopMaps = new List<WorkshopMapInfo>();
    
    private List<GameServer> _gameServers = new List<GameServer>();
    public GameServer _currentGameServer = null!;

    private static Wst _instance;
    public static Wst Instance => _instance;
    
    public Supabase.Client _supabaseClient = null!;
    public RealtimeChannel _chatChannel = null!;
    public RealtimeBroadcast<ChatBroadcast> _broadcast = null!;
    
    public static string NoVipMessage = $" {CC.Main}[oce.surf] {CC.White}You must be a {ChatColors.Purple}VIP{CC.White} to use this command. Visit {CC.Secondary}https://oce.surf/vip {CC.White} to get VIP";

    public void InitTimer()
    {
        _entityManager = new EntityManager();
        _eventManager = new EventManager();
        _database = new Database();

        _entityManager.AddSystem(new ConnectionSystem(_eventManager, _entityManager, _database));
        _entityManager.AddSystem(new StartZoneSystem(_eventManager, _entityManager, _database));
        _entityManager.AddSystem(new EndZoneSystem(_eventManager, _entityManager, _database));
        _entityManager.AddSystem(new CheckpointZoneSystem(_eventManager, _entityManager, _database));
        _entityManager.AddSystem(new HudSystem(_eventManager, _entityManager, _database));
        _entityManager.AddSystem(new ReplayRecorderSystem(_eventManager, _entityManager, _database));
        _entityManager.AddSystem(new ReplayPlaybackSystem(_eventManager, _entityManager, _database));
        _entityManager.AddSystem(new DiscordSystem(_eventManager, _entityManager, _database));
        _entityManager.AddSystem(new AdvertisingSystem(_eventManager, _entityManager, _database));
        _entityManager.AddSystem(new ServerSystem(_eventManager, _entityManager, _database));
        _entityManager.AddSystem(new ClanTagSystem(_eventManager, _entityManager, _database));
        _entityManager.AddSystem(new TurbomasterSystem(_eventManager, _entityManager, _database));
        _entityManager.AddSystem(new SidewaysSurfSystem(_eventManager, _entityManager, _database));
        _entityManager.AddSystem(new HSWSurfSystem(_eventManager, _entityManager, _database));
        
        _ = InitAsync(Server.GameDirectory);
    }

    private async Task InitAsync(string gameDir)
    {
        try
        {
            var options = new Supabase.SupabaseOptions
            {
                AutoConnectRealtime = true
            };
            
            _supabaseClient =  new Supabase.Client(SupabaseUrl, SupabaseToken, options);
            
            await _supabaseClient.InitializeAsync();
            _chatChannel = _supabaseClient.Realtime.Channel("chat");
            
            _broadcast = _chatChannel.Register<ChatBroadcast>();
            _broadcast.AddBroadcastEventHandler((sender, baseBroadcast) =>
            {
                try
                {
                    var response = _broadcast.Current();
                    Server.NextFrame(() =>
                    {
                        Server.PrintToChatAll(Utils.ColorNamesToTags(response.Message));
                    });
                }
                catch (Exception e)
                {
                    Console.WriteLine("ERROR:");
                    Console.WriteLine(e);
                }
                
                
            });
            
            await _chatChannel.Subscribe();
            
            var gameServers = await _database.QueryAsync<GameServer>("select server_id, workshop_collection, hostname, is_public, ip, current_map, real_ip, player_count, total_players, short_name, style from servers");
            
            var serverConfigFile = "WST/server.json";
            var serverConfigPath = Path.Join(gameDir + "/csgo/cfg", serverConfigFile);
            
            // read file
            var serverConfigJson = File.ReadAllText(serverConfigPath);
            var serverConfig = JsonSerializer.Deserialize<ServerConfig>(serverConfigJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            
            var server = gameServers.FirstOrDefault(x => x.ServerId == serverConfig.Id);
            
            _gameServers = gameServers.ToList();
            
            Console.WriteLine("FOUND:");
            Console.WriteLine(JsonSerializer.Serialize(gameServers));
            Console.WriteLine(JsonSerializer.Serialize(serverConfig));
            Console.WriteLine(JsonSerializer.Serialize(server));
            
            if (server == null)
            {   
                Console.WriteLine($"Server not found in database: {serverConfig.Id}");
                return;
            }
            
            _currentGameServer = server;
            Server.NextFrame(() =>
            {
                _eventManager.Publish(new OnServerStatusUpdateEvent()
                {
                    ServerId = _currentGameServer.ServerId
                });
            });
            
            var workshopMaps = await Workshop.LoadMapPool(server.WorkshopCollection);
            _workshopMaps = workshopMaps;

            // "Maplist"
            // {
            //     "ze_my_first_ze_map"
            //     {
            //         "workshop_id" "123"
            //         "enabled" "1"
            //     }
            //     "ze_my_second_ze_map"
            //     {
            //         "workshop_id" "456"
            //         "enabled" "1"
            //     }
            //     "ze_my_thirdd_ze_map"
            //     {
            //         "workshop_id" "789"
            //         "enabled" "1"
            //     }
            // }

            // C:\cs2\game\csgo\addons\cs2fixes\configs
            var mapListPath = Path.Combine(gameDir, "csgo", "addons", "cs2fixes", "configs", "maplist.cfg");
            
            // make folder if it doesn't exist
            var mapListFolder = Path.GetDirectoryName(mapListPath);
            if (!Directory.Exists(mapListFolder))
            {
                Directory.CreateDirectory(mapListFolder);
            }
            

            var newMapList = new List<string>();
            newMapList.Add("\"Maplist\"");
            newMapList.Add("{");
            foreach (var map in workshopMaps)
            {
                newMapList.Add($"\"{map.Title}\"");
                newMapList.Add("{");
                newMapList.Add($"\"workshop_id\" \"{map.Publishedfileid}\"");
                newMapList.Add($"\"enabled\" \"1\"");
                newMapList.Add("}");
            }

            newMapList.Add("}");

            File.WriteAllLines(mapListPath, newMapList);
            
            Server.NextFrame(() =>
            {
                Server.ExecuteCommand("c_reload_map_list");
        
            });
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }


    public async Task ExecuteMapCfg(string mapName)
    {
        Server.NextFrame(() =>
        {
            Server.ExecuteCommand("mp_humanteam CT");
            Server.ExecuteCommand("bot_join_team CT");
            Server.ExecuteCommand("bot_quota 1");
            Server.ExecuteCommand("bot_quota_mode normal");
            Server.ExecuteCommand("bot_stop 1");
            Server.ExecuteCommand("bot_freeze 1");
            Server.ExecuteCommand("bot_zombie 1");
            
        });
        var globalCfg = await _database.QueryAsync<GlobalCfg>("SELECT * FROM global_cfg");

        var mapCfg = await _database.QueryAsync<MapCfg>("SELECT * FROM map_cfg WHERE mapname = @mapname",
        new { mapname = mapName });
        
        Server.NextFrame(() =>
        {
            AddTimer(5.0f, () =>
            {
                _entityManager.RemoveEntity<SurfBot>();
                var playerEntities = Utilities.FindAllEntitiesByDesignerName<CCSPlayerController>("cs_player_controller");
                foreach (var playerEntity in playerEntities)
                {
                    if (!playerEntity.IsBot) continue;
                    
                    var evt = new WST_EventBotConnect
                    {
                        Slot = playerEntity.Slot,
                    };
                    _eventManager.Publish(evt);
                    _eventManager.Publish(new EventOnLoadBot { MapName = Server.MapName, Style = _currentGameServer.Style });
                }
            });
            
            if (globalCfg.Count() > 0)
            {
                var cfg = globalCfg.First();
                Console.WriteLine($"Executing global cfg");
                // parse the commands by splitting on new lines
                var commands = cfg.Command.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
                // remove empty lines
                commands = commands.Where(x => !String.IsNullOrEmpty(x)).ToArray();
                foreach (var command in commands)
                {
                    Console.WriteLine($"Executing global cfg command {command}");
                    Server.ExecuteCommand(command);
                }
            }

            if (mapCfg.Count() > 0)
            {
                foreach (var cfg in mapCfg)
                {
                    Console.WriteLine($"Executing map cfg {cfg.Command}");
                    Server.ExecuteCommand(cfg.Command);
                }
            }
        });
       
    }
    


    public override void Load(bool hotReload)
    {
        if (_instance == null) _instance = this;
        
      
        // confusing naming, this hsould be WILLS SURF TIMER
        InitTimer();
        
        // this means counterstrikesharp timer
        AddTimer(45f, () =>
        {
            _eventManager.Publish(new OnAdvertisingTimerTickEvent());
            if (_currentGameServer != null)
            {
                _eventManager.Publish(new OnServerStatusUpdateEvent
                {
                    ServerId = _currentGameServer.ServerId
                });
            }
        }, TimerFlags.REPEAT);
        
        RegisterEventHandler<EventPlayerConnectFull>((@event, info) =>
        {
            Console.WriteLine("Events.EventPlayerConnectFull");
            var player = @event.Userid;
            if (player == null || !player.IsValid || player.IsBot)
            {
                Console.WriteLine($"[EventPlayerConnectFull] Player is null or invalid");
                return HookResult.Continue;
            }
            
            var steamid = player.NativeSteamId3();
            var slot = player.Slot;
            var name = player.PlayerName;
            var mapName = Server.MapName;

            var e = new WST_EventPlayerConnect
            {
                SteamId = steamid,
                Slot = slot,
                Name = name,
                MapName = mapName,
                Style = _currentGameServer.Style
            };
            _eventManager.Publish(e);
            return HookResult.Continue;
        });

        RegisterEventHandler<EventPlayerDisconnect>((@event, info) =>
        {
            Console.WriteLine("Events.EventPlayerDisconnect");
            var player = @event.Userid;
            if (player == null || !player.IsValid)
            {
                Console.WriteLine($"[EventPlayerDisconnectFull] Player is null, bot, or invalid");
                return HookResult.Continue;
            }
            // only player
            var entity = _entityManager.FindEntity<SurfPlayer>(player.Slot.ToString());
            // is either bot or player
            
            _entityManager.RemoveEntity<SurfEntity>(player.Slot.ToString());

            // fire event for player only
            if (entity != null && !player.IsBot)
            {
                var steamid = player.NativeSteamId3();
                var slot = player.Slot;
                var name = player.PlayerName;
                var mapName = Server.MapName;

                var e = new WST_EventPlayerDisconnect()
                {
                    SteamId = steamid,
                    Slot = slot,
                    Name = name,
                    MapName = mapName,
                    Entity = entity
                };
                _eventManager.Publish(e);
            }

           
            return HookResult.Continue;
        });
        
        RegisterEventHandler<EventRoundStart>((@event, info)  =>
        {
            AddTimer(1.0f, () =>
            {
                _ = ExecuteMapCfg(Server.MapName);
                foreach (var player in Utilities.FindAllEntitiesByDesignerName<CCSPlayerController>("cs_player_controller"))
                {
                    if (player.IsBot)
                    {
                        continue;
                    }
                    var entity = _entityManager.FindEntity<SurfPlayer>(player.Slot.ToString());
                    if (entity == null)
                    {
                        continue;
                    }
                    Restart(player);
                }

            }, TimerFlags.STOP_ON_MAPCHANGE);
            return HookResult.Continue;
        });



        RegisterEventHandler<EventPlayerSpawn>((@event, info) =>
        {
            Console.WriteLine("Events.EventPlayerSpawn");
            var player = @event.Userid; 
            if (player == null)
            {
                Console.WriteLine($"[EventPlayerSpawn] Player is null, bot, or invalid");
                return HookResult.Continue;
            }
            if (!player.IsValid)
            {
                Console.WriteLine($"[EventPlayerSpawn] Player is null, bot, or invalid");
                return HookResult.Continue;
            }
            if (player.IsBot)
            {
                return HookResult.Continue;
            }
            
            var entity = _entityManager.FindEntity<SurfPlayer>(player.Slot.ToString());
            if (entity == null)
            {
                return HookResult.Continue;
            }

            Restart(player);
            AddTimer(1.5f, () =>
            {
                _eventManager.Publish(new UpdateClanTagEvent
                {
                    PlayerSlot = player.Slot
                });

            });
            return HookResult.Continue;
        });

        RegisterEventHandler<EventPlayerDeath>((@event, info) =>
        {
            Console.WriteLine("Events.EventPlayerDeath");
            var player = @event.Userid;
            if (player == null)
            {
                Console.WriteLine($"[EventPlayerDeath] Player is null, bot, or invalid");
                return HookResult.Continue;
            }

            if (!player.IsValid)
            {
                Console.WriteLine($"[EventPlayerDeath] Player is null, bot, or invalid");
                return HookResult.Continue;
            }

            if (player.IsBot)
            {
                return HookResult.Continue;
            }

            var entity = _entityManager.FindEntity<SurfPlayer>(player.Slot.ToString());
            if (entity == null)
            {
                return HookResult.Continue;
            }
            AddTimer(1.5f, () =>
            {
                _eventManager.Publish(new UpdateClanTagEvent
                {
                    PlayerSlot = player.Slot
                });
            });
            return HookResult.Continue;
        });

        var commandListenerCallback = (CCSPlayerController? player, CommandInfo info, bool isAllChat) =>
        {
            if (player == null || !player.IsValid || info.GetArg(1).Length == 0) return HookResult.Continue;
            var entity = _entityManager.FindEntity<SurfPlayer>(player.Slot.ToString());
            if (entity == null)
            {
                return HookResult.Continue;
            }
            
            var playerInfo = entity.GetComponent<PlayerComponent>();
            if (playerInfo == null)
            {
                return HookResult.Continue;
            }
            
            var skillGroup = playerInfo.SkillGroup;
            if (skillGroup == null)
            {
                return HookResult.Continue;
            }
            
            if (info.GetArg(1).StartsWith("!") || info.GetArg(1).StartsWith("/")) return HookResult.Continue;

            Console.WriteLine("COMMAND: " + info.GetCommandString);
            
            string message = "";
            if (info.ArgCount == 2)
            {
                message = info.GetArg(1);
                Console.WriteLine("ARG2: " + message);
            }
            else
            {
                var fullMessage = info.GetCommandString;
                fullMessage = fullMessage.Replace("say_team ", "");
                fullMessage = fullMessage.Replace("say ", "");
                
                Console.WriteLine("FULL: " + fullMessage);
                message = fullMessage;
            }

            if (playerInfo.PlayerStats.IsVip)
            {
                if (playerInfo.PlayerStats.ChatColor != null)
                {
                    message = $"{playerInfo.PlayerStats.ChatColor}{message}";
                }
                message = Utils.ColorNamesToTags(message);
            }
            else
            {
                message = Utils.RemoveColorNames(message);
            }
            
            var playerName = player.PlayerName;

            if (playerInfo.PlayerStats.IsVip && playerInfo.PlayerStats.NameColor != null)
            {
                playerName = $"{Utils.ColorNamesToTags(playerInfo.PlayerStats.NameColor)}{playerName}";
            }
            else
            {
                if (player.TeamNum == (int)CsTeam.Spectator)
                {
                    playerName = $"{ChatColors.Default}{playerName}";
                }
                else
                {
                    playerName = $"{ChatColors.BlueGrey}{playerName}";
                }
                
            }
            
            
            if (player.TeamNum == (int)CsTeam.Spectator)
            {
                var localMsg =
                    $" {playerInfo.ChatRankFormatted()} {playerName} {ChatColors.Grey}[SPEC]{ChatColors.Default}: {message}";
                if (isAllChat)
                {
                    var globalMsg =
                        $" {ChatColors.LightPurple}({_currentGameServer.ShortName}) {playerInfo.ChatRankFormatted()} {playerName} {ChatColors.Grey}[SPEC]{ChatColors.Default}: {message}";
                    _ = _broadcast.Send("chat", new ChatBroadcast { Message = Utils.TagsToColorNames(globalMsg) });
                }
                Server.PrintToChatAll(localMsg);
                return HookResult.Handled;
            }

            var localMsg2 =
                $" {playerInfo.ChatRankFormatted()} {playerName}{ChatColors.Default}: {message}";
            if (isAllChat)
            {
                var globalMsg2 =
                    $" {ChatColors.LightPurple}({_currentGameServer.ShortName}) {playerInfo.ChatRankFormatted()} {playerName}{ChatColors.Default}: {message}";
                _ = _broadcast.Send("chat", new ChatBroadcast { Message = Utils.TagsToColorNames(globalMsg2) });
            }
            Server.PrintToChatAll(localMsg2);
            
            return HookResult.Handled;
        };
        AddCommandListener("say", ((player, info) =>
        {
            return commandListenerCallback(player, info, true);
        }));
        AddCommandListener("say_team",((player, info) =>
        {
            return commandListenerCallback(player, info, false);
        }));

        HookEntityOutput("trigger_multiple", "OnStartTouch", (CEntityIOOutput output, string name,
            CEntityInstance activator, CEntityInstance caller, CVariant value, float delay) =>
        {
            if (activator == null || caller == null)
            {
                return HookResult.Continue;
            }

            if (activator.DesignerName != "player")
            {
                return HookResult.Continue;
            }

            var player = new CCSPlayerController(new CCSPlayerPawn(activator.Handle).Controller.Value.Handle);

            if (!player.NativeIsValidAliveAndNotABot())
            {
                return HookResult.Continue;
            }

            var triggerName = caller.Entity.Name.ToString();
            var playerSlot = player.Slot;

            _eventManager.Publish(new OnStartTouchEvent { TriggerName = triggerName, PlayerSlot = playerSlot });
            return HookResult.Continue;
        });

        HookEntityOutput("trigger_multiple", "OnEndTouch", (CEntityIOOutput output, string name,
            CEntityInstance activator, CEntityInstance caller, CVariant value, float delay) =>
        {
            if (activator == null || caller == null)
            {
                return HookResult.Continue;
            }
            
            if (activator.DesignerName != "player")
            {
                return HookResult.Continue;
            }


            var player = new CCSPlayerController(new CCSPlayerPawn(activator.Handle).Controller.Value.Handle);

            if (!player.NativeIsValidAliveAndNotABot())
            {
                return HookResult.Continue;
            }

            var triggerName = caller.Entity.Name.ToString();
            var playerSlot = player.Slot;

            _eventManager.Publish(new OnEndTouchEvent { TriggerName = triggerName, PlayerSlot = playerSlot });
            return HookResult.Continue;
        });
        
        RegisterListener<Listeners.OnClientConnected>((int playerSlot) =>
        {
            Console.WriteLine("Listeners.OnClientConnected");
        });

        RegisterListener<Listeners.OnMapStart>(map =>
        {
            Console.WriteLine("Listeners.OnMapStart");
            AddTimer(3.0f, () =>
            {
                MapLoad();
                if (_currentGameServer != null)
                {
                    _eventManager.Publish(new OnServerStatusUpdateEvent
                    {
                        ServerId = _currentGameServer.ServerId
                    });
                }
            }, TimerFlags.STOP_ON_MAPCHANGE);
            
            AddTimer(1.0f, () =>
            {
                
            }, TimerFlags.REPEAT | TimerFlags.STOP_ON_MAPCHANGE);
        });
        
        RegisterListener<Listeners.OnMapEnd>(() =>
        {
            Console.WriteLine("Listeners.OnMapStart");
            _entityManager.RemoveEntity<SurfEntity>();
            _entityManager.RemoveEntity<Map>();
        });

        RegisterListener<Listeners.OnTick>(() => { _eventManager.Publish(new OnTickEvent()); });
    }
    
    void OnStageSelectHandler(CCSPlayerController player, CommandInfo info)
    {
        if (player == null)
        {
            return;
        }
        
        var newRoute = info.GetArg(0);

        var entity = _entityManager.FindEntity<SurfPlayer>(player.Slot.ToString());
        var playerInfo = entity.GetComponent<PlayerComponent>();
        
        var map = _entityManager.FindEntity<Map>();
        var mapZones = map.GetComponent<MapZone>();
        var route = mapZones.GetRoute(newRoute);
        if (route == null)
        {
            player.PrintToChat($" {CC.White}Route not found");
            return;
        }
        playerInfo.ChangeRoute(newRoute);
        

        // Changed route to X
        player.PrintToChat($" {CC.White}Changed route to {CC.Secondary}{route.Name}");
        Restart(player);
    }

    public class TriggerInfo
    {
        public string TargetName { get; set; }
        public ZoneVector Position { get; set; }
        public ZoneVector Mins { get; set; }
        public ZoneVector Maxs { get; set; }
        
        public bool IsInside(ZoneVector pos)
        {
            // function CalculateVectorsFromBox(center, mins, maxs)
            //     -- Re-adjust mins and maxs to be absolute rather than relative to the center
            // local absoluteMins = center + mins
            // local absoluteMaxs = center + maxs
            //
            //     -- Determine the original vectors v1 and v2
            // local v1 = Vector(math.min(absoluteMins.x, absoluteMaxs.x), math.min(absoluteMins.y, absoluteMaxs.y), math.min(absoluteMins.z, absoluteMaxs.z))
            // local v2 = Vector(math.max(absoluteMins.x, absoluteMaxs.x), math.max(absoluteMins.y, absoluteMaxs.y), math.max(absoluteMins.z, absoluteMaxs.z))
            //
            // return v1, v2
            // end
            
            // function IsPointInBox(point, minVec, maxVec)
            // return (point.x >= minVec.x and point.x <= maxVec.x) and
            //     (point.y >= minVec.y and point.y <= maxVec.y) and
            //     (point.z >= minVec.z and point.z <= maxVec.z)
            // end
            
            var absoluteMins = new ZoneVector
            {
                x = Position.x + Mins.x,
                y = Position.y + Mins.y,
                z = Position.z + Mins.z
            };
            
            var absoluteMaxs = new ZoneVector
            {
                x = Position.x + Maxs.x,
                y = Position.y + Maxs.y,
                z = Position.z + Maxs.z
            };
            
            var v1 = new ZoneVector
            {
                x = Math.Min(absoluteMins.x, absoluteMaxs.x),
                y = Math.Min(absoluteMins.y, absoluteMaxs.y),
                z = Math.Min(absoluteMins.z, absoluteMaxs.z)
            };
            
            var v2 = new ZoneVector
            {
                x = Math.Max(absoluteMins.x, absoluteMaxs.x),
                y = Math.Max(absoluteMins.y, absoluteMaxs.y),
                z = Math.Max(absoluteMins.z, absoluteMaxs.z)
            };
            
            return (pos.x >= v1.x && pos.x <= v2.x) &&
                   (pos.y >= v1.y && pos.y <= v2.y) &&
                   (pos.z >= v1.z && pos.z <= v2.z);
        }
    }
    

    private void MapLoad()
    {
        _entityManager.RemoveEntity<Map>();
        Dictionary<string, TriggerInfo> triggersByName = new Dictionary<string, TriggerInfo>();
        foreach (var trigger in Utilities.FindAllEntitiesByDesignerName<CBaseTrigger>("trigger_multiple"))
        {
            var name = trigger.Entity!.Name;
            if (name == null)
            {
                continue;
            }

            Console.WriteLine($"Found trigger {name} with handle {trigger.Handle}");

            if (triggersByName.ContainsKey(name))
            {
                Console.WriteLine($"Trigger {name} already exists");
                continue;
            }
            
            var triggerInfo = new TriggerInfo
            {
                TargetName =  trigger.Entity!.Name,
                Position = new ZoneVector
                {
                    x = trigger.AbsOrigin.X, y = trigger.AbsOrigin.Y, z = trigger.AbsOrigin.Z
                },
                Mins = new ZoneVector
                {
                    x = trigger.Collision.Mins.X, y = trigger.Collision.Mins.Y, z = trigger.Collision.Mins.Z
                },
                Maxs = new ZoneVector
                {
                    x = trigger.Collision.Maxs.X, y = trigger.Collision.Maxs.Y, z = trigger.Collision.Maxs.Z
                }
            };


            triggersByName.Add(name, triggerInfo);
        }

        var mapName = Server.MapName;
        Task.Run(async () =>
        {
            try
            {
                var mapZone = await LoadOrCreateMapZones(_database, mapName, triggersByName);
              
                Server.NextFrame(() =>
                {
                    var mapEntity = new Map { Id = Server.MapName };
                    _entityManager.AddEntity(mapEntity);
                    if (mapZone == null)
                    {
                        return;
                    }

                    mapEntity.AddComponent(mapZone);

                    var addHandlesToRoute = new Action<Route>(route =>
                    {
                        route.Start.TriggerInfo = triggersByName[route.Start.TargetName!];
                        route.End.TriggerInfo = triggersByName[route.End.TargetName!];
                        foreach (var checkpoint in route.Checkpoints)
                        {
                            checkpoint.TriggerInfo = triggersByName[checkpoint.TargetName!];
                        }
                    });

                    Server.ExecuteCommand("wst_create_trigger_zone " + mapZone.Main.Start.TargetName + " 0 255 0");
                    Server.ExecuteCommand("wst_create_trigger_zone " + mapZone.Main.End.TargetName + " 255 0 0");

                    foreach (var checkpoint in mapZone.Main.Checkpoints)
                    {
                        Server.ExecuteCommand("wst_create_trigger_zone " + checkpoint.TargetName + " 0 0 255");
                    }

                    _ = mapZone.LoadRecords(_database, mapName);

                    foreach (var route in mapZone.Routes)
                    {
                        addHandlesToRoute(route);
                    }

                    foreach (var route in mapZone.Routes)
                    {
                        if (!_dynamicCommands.Contains(route.Key))
                        {
                            AddCommand(route.Key, route.Name, OnStageSelectHandler);
                            _dynamicCommands.Add(route.Key);
                        }
                        Console.WriteLine($"Added command {route.Key}");
                    }
                });
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        });
    }

    private static async Task<MapZone?> LoadOrCreateMapZones(Database database, string mapName,
        Dictionary<string, TriggerInfo> triggersByName)
    {
        var result =
            (await database.QueryAsync<MapTable>("SELECT * FROM maps_2 WHERE name = @name", new { name = mapName }));

        if (result.Count() >= 1)
        {
            Console.WriteLine("Loading mapzone from db");
            var routes = new List<Route>();
            foreach (var mapTable in result)
            {
                var routeData = JsonSerializer.Deserialize<Route>(
                    mapTable.RouteData,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                routes.Add(routeData);
            }

            return new MapZone
            {
                Routes = routes
            };
        }

        Console.WriteLine("Creating mapzone from triggers");
        var mapZone = new MapZone();
        mapZone.Routes = new List<Route>();

        var startZoneNames = new List<string> { "map_start", "s1_start", "stage1_start" };
        var endZoneNames = new List<string> { "map_end" };

        // Does the map have a startzone and a endzone?
        var triggerNames = triggersByName.Keys.ToList();
        var startZoneName = triggerNames.FirstOrDefault(x => startZoneNames.Contains(x));
        var endZoneName = triggerNames.FirstOrDefault(x => endZoneNames.Contains(x));
        if (startZoneName == null || endZoneName == null)
        {
            Console.WriteLine("Map does not have a startzone or endzone");
            return null;
        }

        var startZonePosition = triggersByName[startZoneName].Position;

        var route = new Route();
        route.Key = "main";
        route.Name = "Main";
        route.Start = new Zone { TargetName = startZoneName, Type = ZoneType.trigger };
        route.End = new Zone { TargetName = endZoneName, Type = ZoneType.trigger };
        route.StartPos = new ZoneVector { x = startZonePosition.x, y = startZonePosition.y, z = startZonePosition.z };
        route.StartVelocity = new ZoneVector { x = 0, y = 0, z = 0 };
        route.StartAngles = new ZoneVector { x = 0, y = 0, z = 0 };

        route.Checkpoints = new List<Zone>();

        // (s)tageX_start
        // ^s(tage)?[1-9][0-9]?_start
        var stageRegex = new Regex(@"^s(tage)?[1-9][0-9]?_start$");
        // store a list of <int, string> where int is the stage number and string is the trigger name
        var stageStarts = new List<(int, string)>();
        foreach (var triggerName in triggerNames)
        {
            var match = stageRegex.Match(triggerName);
            if (!match.Success)
            {
                continue;
            }

            if (startZoneNames.Contains(triggerName))
            {
                continue;
            }

            var stageNumber = Int32.Parse(Regex.Match(triggerName, "[0-9][0-9]?").Value);
            stageStarts.Add((stageNumber, triggerName));
        }

        stageStarts.Sort((x, y) => x.Item1.CompareTo(y.Item1));
        foreach (var stageStart in stageStarts)
        {
            route.Checkpoints.Add(new Zone { TargetName = stageStart.Item2, Type = ZoneType.trigger });
        }

        // map_(c)heck(p)ointX
        // ^map_(cp|checkpoint)[1-9][0-9]?$
        var checkpointRegex = new Regex(@"^map_(cp|checkpoint)[1-9][0-9]?$");
        // store a list of <int, string> where int is the checkpoint number and string is the trigger name
        var checkpointStarts = new List<(int, string)>();
        foreach (var triggerName in triggerNames)
        {
            var match = checkpointRegex.Match(triggerName);
            if (!match.Success)
            {
                continue;
            }

            var checkpointNumber = Int32.Parse(Regex.Match(triggerName, "[0-9][0-9]?").Value);
            checkpointStarts.Add((checkpointNumber, triggerName));
        }

        checkpointStarts.Sort((x, y) => x.Item1.CompareTo(y.Item1));
        foreach (var checkpointStart in checkpointStarts)
        {
            route.Checkpoints.Add(new Zone { TargetName = checkpointStart.Item2, Type = ZoneType.trigger });
        }

        mapZone.Routes.Add(route);

        // (b)onusX_start
        var bonusStartRegex = new Regex(@"^b(onus)?[1-9][0-9]?_start$");
        var bonusEndRegex = new Regex(@"^b(onus)?[1-9][0-9]?_end$");
        // b)onusX_(c)heck(p)ointY
        var bonusCpRegex = new Regex(@"^b(onus)?([1-9][0-9]?)_(cp|checkpoint)([1-9][0-9]?)$");
        foreach (var triggerName in triggerNames)
        {
            var match = bonusStartRegex.Match(triggerName);
            if (!match.Success)
            {
                continue;
            }

            var number = Int32.Parse(Regex.Match(triggerName, "[0-9][0-9]?").Value);

            var bonusStartVector = triggersByName[triggerName].Position;

            var bonusRoute = new Route();
            bonusRoute.Key = "b" + number;
            bonusRoute.Name = "Bonus " + number;
            bonusRoute.Start = new Zone { TargetName = triggerName, Type = ZoneType.trigger };
            bonusRoute.Checkpoints = new List<Zone>();
            bonusRoute.StartPos = new ZoneVector
                { x = bonusStartVector.x, y = bonusStartVector.y, z = bonusStartVector.z };
            bonusRoute.StartVelocity = new ZoneVector { x = 0, y = 0, z = 0 };
            bonusRoute.StartAngles = new ZoneVector { x = 0, y = 0, z = 0 };

            var bonusCpStarts = new List<(int, string)>();
            foreach (var triggerName2 in triggerNames)
            {
                var bonusEndMatch = bonusEndRegex.Match(triggerName2);
                if (bonusEndMatch.Success)
                {
                    var number2 = Int32.Parse(Regex.Match(triggerName2, "[0-9][0-9]?").Value);
                    if (number != number2)
                    {
                        continue;
                    }

                    bonusRoute.End = new Zone { TargetName = triggerName2, Type = ZoneType.trigger };
                }

                var bonusCpMatch = bonusCpRegex.Match(triggerName2);
                if (bonusCpMatch.Success)
                {
                    var bonusNumber = Int32.Parse(match.Groups[2].Value);
                    var checkpointNumber = Int32.Parse(match.Groups[4].Value);
                    if (bonusNumber != number)
                    {
                        continue;
                    }

                    bonusCpStarts.Add((checkpointNumber, triggerName2));
                }
            }

            bonusCpStarts.Sort((x, y) => x.Item1.CompareTo(y.Item1));
            foreach (var bonusCpStart in bonusCpStarts)
            {
                bonusRoute.Checkpoints.Add(new Zone { TargetName = bonusCpStart.Item2, Type = ZoneType.trigger });
            }

            mapZone.Routes.Add(bonusRoute);
        }

        // Create the stage routes
        // so the first stage route (s1) will be from map_start to s2_start (which is the first stage checkpoint)
        // the last stage route (sX) will be from sX_start to map_end

        if (stageStarts.Count > 0)
        {
            stageStarts = stageStarts.Prepend((1, startZoneName)).ToList();
            stageStarts.Add((stageStarts.Count + 1, endZoneName));
        }

        for (var i = 0; i < stageStarts.Count - 1; i++)
        {
            var stageStart = stageStarts[i];
            var stageEnd = stageStarts[i + 1];

            var startStageVector = triggersByName[stageStart.Item2].Position;

            var stageRoute = new Route();
            stageRoute.Key = "s" + stageStart.Item1;
            stageRoute.Name = "Stage " + stageStart.Item1;
            stageRoute.Start = new Zone { TargetName = stageStart.Item2, Type = ZoneType.trigger };
            stageRoute.End = new Zone { TargetName = stageEnd.Item2, Type = ZoneType.trigger };
            stageRoute.Checkpoints = new List<Zone>();
            stageRoute.StartPos = new ZoneVector
                { x = startStageVector.x, y = startStageVector.y, z = startStageVector.z };
            stageRoute.StartVelocity = new ZoneVector { x = 0, y = 0, z = 0 };
            stageRoute.StartAngles = new ZoneVector { x = 0, y = 0, z = 0 };
            mapZone.Routes.Add(stageRoute);
        }

        // CREATE TABLE IF NOT EXISTS maps_2
        // (
        //     id SERIAL PRIMARY KEY,
        //     name VARCHAR(255) NOT NULL,
        //     route VARCHAR(255) NOT NULL,
        //     route_data JSONB NOT NULL,
        //     UNIQUE(name, route)
        //     );

        await mapZone.Save(database, mapName);

        return mapZone;
    }

    class MapTable
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Route { get; set; }
        public string RouteData { get; set; }
    }
}

