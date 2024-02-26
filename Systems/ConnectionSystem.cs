using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using WST.Game;

namespace WST;


public class WST_EventPlayerConnect
{
    public string SteamId { get; set; }
    public int Slot { get; set; }
    public string Name { get; set; }
    public string MapName { get; set; }
    public string Style { get; set; }
    
}

public class WST_EventPlayerDisconnect
{
    public string SteamId { get; set; }
    public int Slot { get; set; }
    public string Name { get; set; }
    public string MapName { get; set; }
    


    public SurfPlayer Entity {get; set;}
}

public class WST_EventBotConnect
{
    public int Slot { get; set; }
}

public class ConnectionSystem : System
{
    public ConnectionSystem(EventManager eventManager, EntityManager entityManager, Database database)
        : base(eventManager, entityManager, database)
    {
        EventManager.Subscribe<WST_EventPlayerConnect>(OnPlayerConnect);
        EventManager.Subscribe<WST_EventPlayerDisconnect>(OnPlayerDisconnect);
        EventManager.Subscribe<WST_EventBotConnect>(OnBotConnect);
    }

    private void OnBotConnect(WST_EventBotConnect e)
    {
        var slot = e.Slot;
        var mapName = Server.MapName;

        EntityManager.AddEntity(new SurfBot()
        {
            Id = slot.ToString(),
        });
    }

    private void OnPlayerConnect(WST_EventPlayerConnect e)
    {
        var steamid = e.SteamId;
        var slot = e.Slot;
        var name = e.Name;
        var mapName = Server.MapName;
        var style = e.Style;

        Task.Run(async () =>
        {
            try
            {
                await Database.ExecuteAsync(
                    "INSERT INTO players (steam_id, points, name) VALUES (@steamid, 0, @name) ON CONFLICT (steam_id) DO UPDATE SET name = EXCLUDED.name",
                    new { steamid, name });
                
                var playerComponent = new PlayerComponent(slot, steamid, name, style);
                await playerComponent.LoadStats(Database);
                await playerComponent.LoadRecords(Database, mapName);

                EntityManager.AddEntity(new SurfPlayer
                {
                    Id = playerComponent.Slot.ToString(),
                    Components =
                    {
                        { typeof(PlayerComponent), playerComponent }
                    }
                });


                Server.NextFrame(() =>
                {
                    var player = playerComponent.GetPlayer();
                    // player.Clan = playerComponent.SkillGroup.Name.ToUpper();
                    var existingName = player.PlayerName;

                    var rank = playerComponent.StyleRank.ToString();
                    if (playerComponent.SkillGroup.Name.ToLower() == "unranked")
                    {
                        rank = "-";
                    }
                    else
                    {
                        // var playerNameSchema = new SchemaString<CBasePlayerController>(player, "m_iszPlayerName");
                        //
                        //
                        // playerNameSchema.Set($"[#{rank}] {existingName}");
                        
                    }
                    
                    // Utilities.SetStateChanged(player, "CCSPlayerController", "m_szClan");
                    // Utilities.SetStateChanged(player, "CBasePlayerController", "m_iszPlayerName");
                    //UpdatePlayerModel.UpdatePlayer(player); 
                    
                    var pointsString =  $"({ChatColors.Gold}{playerComponent.PlayerStats.PPoints}{ChatColors.White} points) " +
                                  $"(rank {ChatColors.Gold}{rank}{ChatColors.White}/{ChatColors.Gold}{playerComponent.PlayerStats.TotalPlayers}{ChatColors.White})";

                    // Will has joined the server (0 points) (rank 0/0)
                    Server.PrintToChatAll(
                        $" {playerComponent.ChatRankFormatted()} {ChatColors.Gold}{existingName}{ChatColors.White} has joined the server " +
                        pointsString);
                       
                    
                    // (MIXED-AU) Will has joined MIXED-AU on surf_cannonball (0 points) (rank 0/0)
                    var globalMsg =
                        $" {ChatColors.LightPurple}({Wst.Instance._currentGameServer.ShortName}) {playerComponent.ChatRankFormatted()}" +
                        $" {ChatColors.Gold}{player.PlayerName}{ChatColors.White} has joined" +
                        $" {ChatColors.LightPurple}{Wst.Instance._currentGameServer.ShortName}{ChatColors.White} on" +
                        $" {ChatColors.LightPurple}{mapName} " +
                        pointsString;
                    _ = Wst.Instance._broadcast.Send("chat", new ChatBroadcast { Message = Utils.TagsToColorNames(globalMsg) });

                    Wst.Instance.AddTimer(2.0f, () =>
                    {
                        EventManager.Publish(new UpdateClanTagEvent
                        {
                            PlayerSlot = slot
                        });
                    });

                    Wst.Instance.AddTimer(5.0f, () =>
                    {  
                        var playerEntity = EntityManager.FindEntity<SurfPlayer>(slot.ToString());
                        if (playerEntity == null)
                        {
                            return;
                        }

                        var player = playerEntity.GetPlayer();
                        if (player == null || !player.IsValid)
                        {
                            return;
                        }

                        if (style == "tm")
                        {
                            player.PrintToChat(
                                $" {CC.Main}[oce.surf] {ChatColors.White}Welcome to {CC.Secondary}TURBO SURF.");
                            player.PrintToChat(
                                $" {CC.Main}[oce.surf] {CC.Secondary}HOLD LEFT CLICK{ChatColors.White} to go fast.");
                        }
               
                    });
                });
            } catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        });
    }
    
    public void SetPlayerClanTag(int slot, string clanTag)
    {
        var player = Utilities.GetPlayerFromSlot(slot);
        if (player == null) return;
        player.Clan = clanTag;
        // Utilities.SetStateChanged(player, "CCSPlayerController", "m_szClan");
    }

    private void OnPlayerDisconnect(WST_EventPlayerDisconnect e)
    {
        var entity = e.Entity;
        if (entity == null)
        {
            return;
        }

        var mapName = Server.MapName;
        var playerComponent = entity.GetComponent<PlayerComponent>();
        if (playerComponent != null)
        {
            Server.PrintToChatAll($" {playerComponent.ChatRankFormatted()} {ChatColors.Gold}{playerComponent.Name}{ChatColors.White} has left the server");
                    
            // (MIXED-AU) Will has left MIXED-AU on surf_cannonball
            var globalMsg =
                $" {ChatColors.LightPurple}({Wst.Instance._currentGameServer.ShortName}) {playerComponent.ChatRankFormatted()}" +
                $" {ChatColors.Gold}{playerComponent.Name}{ChatColors.White} has left " +
                $" {ChatColors.LightPurple}{Wst.Instance._currentGameServer.ShortName}{ChatColors.White} on" +
                $" {ChatColors.LightPurple}{mapName}";
            
            _ = Wst.Instance._broadcast.Send("chat", new ChatBroadcast { Message = Utils.TagsToColorNames(globalMsg) });
            return;
        }
       
    }
}