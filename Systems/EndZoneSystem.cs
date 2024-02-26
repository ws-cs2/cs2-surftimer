using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Utils;
using Supabase;
using NpgsqlTypes;

namespace WST;

public class RecordDetails
{
    public int PlayerSlot { get; set; }
    public string MapName { get; set; }
    public string Route { get; set; }
    public string SteamId { get; set; }
    public TimerData Timer { get; set; }
    public string Name { get; set; }
    public byte[] ReplayBytes { get; set; }
    public Route MapRoute { get; set; } // Assuming 'Route' is a class that contains details about the map route.
    public bool IsSecondary { get; set; }
    
    public bool IsVip { get; set; }
    
    public string Style { get; set; }

    public string ToStyleDisplayString()
    {
        if (Style == "tm")
        {
            return "turbo";
        }

        return Style;
    }
}

public class EndZoneSystem : System
{

    
    public EndZoneSystem(EventManager eventManager, EntityManager entityManager, Database database)
        : base(eventManager, entityManager, database)
    {
        EventManager.Subscribe<OnStartTouchEvent>(OnStartTouch);
        EventManager.Subscribe<OnEndTouchEvent>(OnEndTouch);
    }
    
    public void OnStartTouch(OnStartTouchEvent e)
    {
        var entity = EntityManager.FindEntity<SurfPlayer>(e.PlayerSlot.ToString());
        var playerInfo = entity.GetComponent<PlayerComponent>();
        if (playerInfo == null) return;
        if (playerInfo.Teleporting)
        {
            // remove timer
            entity.RemoveComponent<PlayerTimerComponent>();
            return;
        }

        var player = playerInfo.GetPlayer();
        var timerComponent = entity.GetComponent<PlayerTimerComponent>();

        var map = EntityManager.FindEntity<Map>();
        var mapZones = map.GetComponent<MapZone>();
        if (mapZones == null) return;
        var route = mapZones.GetRoute(playerInfo.RouteKey);
        if (route == null) return;

        if (!player.NativeIsValidAliveAndNotABot()) return;

        if (timerComponent == null) return;
        
        if (route.End.TargetName == e.TriggerName)
        {
            // Finishing the main route. Do this shit first.
            entity.RemoveComponent<PlayerTimerComponent>();
            FinishTimer(e.PlayerSlot, timerComponent.Primary, player, route, playerInfo, false, playerInfo.InPrac);

            if (playerInfo.RepeatMode)
            {
                // Does this need to be next frame? maybe lets just chill a frame because like alot of shit happening after
                // here
                Server.NextFrame(() =>
                {
                    Wst.Instance.Restart(player);
                });
            }
        };
        
        // now lets see if we are in a secondary route
        
        if (timerComponent.Secondary == null) return;
        
        var secondaryTimer = timerComponent.Secondary;
        var secondaryRouteKey = secondaryTimer.RouteKey;
        var secondaryRoute = mapZones.GetRoute(secondaryRouteKey);
        if (secondaryRoute == null) return;

        if (secondaryRoute.End.TargetName == e.TriggerName)
        {
            FinishTimer(e.PlayerSlot, timerComponent.Secondary, player, secondaryRoute, playerInfo, true, playerInfo.InPrac);
        }
    }
    
    private void FinishTimer(int playerSlot, TimerData timer, CCSPlayerController player, Route route, PlayerComponent playerInfo, bool isSecondary, bool isPrac)
    {
        var endVelocity = new Vector(player.PlayerPawn.Value.AbsVelocity.X, player.PlayerPawn.Value.AbsVelocity.Y,
            player.PlayerPawn.Value.AbsVelocity.Z);

        timer.VelocityEndXY = endVelocity.Length2D();
        timer.VelocityEndZ = endVelocity.Z;
        timer.VelocityEndXYZ = endVelocity.Length();
        
        var checkpointsInRoute = route.Checkpoints.Count;
        var checkpointsHit = timer.Checkpoints.Count;

        if (checkpointsHit != checkpointsInRoute)
        {
            player.PrintToChat(
                $" {ChatColors.Red}Invalid run, you hit {checkpointsHit} checkpoints out of {checkpointsInRoute} total");
            return;
        }

        var mapName = Server.MapName;
        var steamId = player.NativeSteamId3();
        var name = playerInfo.Name;

        var replay = timer?.Recording;
        var replayBytes = replay.Serialize();
        
        var details = new RecordDetails
        {
            PlayerSlot = playerSlot,
            MapName = mapName,
            Route = route.Key,
            SteamId = steamId,
            Timer = timer,
            Name = name,
            ReplayBytes = replayBytes,
            MapRoute = route,
            IsSecondary = isSecondary,
            IsVip = playerInfo.PlayerStats.IsVip,
            Style = playerInfo.Style
        };

        Task.Run(async () =>
        {
            await SavePlayerRecord(details);
        });
    }

    public void OnEndTouch(OnEndTouchEvent e)
    {
    }


    public async Task DeleteReplayFromS3(string url)
    {
        Console.WriteLine("Deleting replay");
        var supabase = Wst.Instance._supabaseClient;
        
        url = url.Replace("/replay", "");
        
        var result = await supabase.Storage.From("replays").Remove(url);
        
        Console.WriteLine("Replay deleted " + url);
    }

    public async Task<string> SaveReplayToS3(Replay replay, string mapName, string route, string steamId, string style)
    {
        try
        {
            // -- ALTER TABLE replays
            // -- ADD CONSTRAINT replays_steam_id_mapname_route_key UNIQUE (steam_id, mapname, route);
            //
            // DELETE FROM replays r
            // WHERE NOT EXISTS (
            //     SELECT 1
            // FROM records rec
            //     WHERE r.steam_id = rec.steam_id
            // AND r.mapname = rec.mapname
            // AND r.route = rec.route
            // AND r.ticks = rec.ticks
            //     );
            var supabase = Wst.Instance._supabaseClient;

            var replayString = replay.SerializeString();

            using var uncompressedStream = new MemoryStream(Encoding.UTF8.GetBytes(replayString));
            using var compressedStream = new MemoryStream();
            using (var compressor = new GZipStream(compressedStream, CompressionMode.Compress, true))
            {
                uncompressedStream.CopyTo(compressor);
            }

            var compressedBytes = compressedStream.ToArray();

            var unixTimeStamp = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds();

            var steamId64 = Utils.SteamId3ToSteamId64(steamId);

            var filename = $"{mapName}/{route}/{style}/{steamId64}-{unixTimeStamp}.json.gz";
            Console.WriteLine("Saving " + filename);

            var result = await supabase.Storage.From("replays").Upload(
                compressedBytes,
                filename,
                new Supabase.Storage.FileOptions
                {
                    Upsert = true,
                    ContentType = "application/gzip",
                }
            );

            return result;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw e;
        }
    }

    // private async Task MigrateReplayToS3()
    // {
    //     var limit = 20;
    //     var skip = 0;
    //     
    //     var replaysCount = (await Database.QueryAsync<int>("SELECT COUNT(*) FROM v_replays WHERE replay_url IS NULL")).FirstOrDefault();
    //
    //     do
    //     {
    //         var replays = await Database.QueryAsync<V_Replay>(
    //             "SELECT * FROM v_replays WHERE replay_url IS NULL LIMIT @limit OFFSET @skip",
    //             new { limit, skip });
    //
    //         foreach (var replay in replays)
    //         {
    //             var replayGameObj = Replay.Deserialize(replay.Replay);
    //             var url = await SaveReplayToS3(replayGameObj, replay.MapName, replay.Route, replay.SteamId);
    //             await Database.ExecuteAsync(
    //                 "UPDATE replays SET replay_url = @url WHERE id = @id",
    //                 new { url, id = replay.Id });
    //         }
    //         
    //         
    //         
    //         skip += limit;
    //         Console.WriteLine("Done: " + skip);
    //     } while (skip < replaysCount);
    //    
    // }

    private async Task<V_RankedRecord> GetWorldRecord(string mapName, string route, string style)
    {
        return (await Database.QueryAsync<V_RankedRecord>(
            "SELECT * FROM v_ranked_records WHERE mapname = @mapName and route = @route and style = @style ORDER BY ticks ASC LIMIT 1",
            new { mapName, route, style })).FirstOrDefault();
    }
    
    private async Task<V_RankedRecord> GetPersonalRecord(string mapName, string route, string steamId, string style)
    {
        return (await Database.QueryAsync<V_RankedRecord>(
            "SELECT * FROM v_ranked_records WHERE mapname = @mapName and route = @route and style = @style and steam_id = @steamId",
            new { mapName, route, steamId, style })).FirstOrDefault();
    }

    private async Task InsertOrUpdateRecord(RecordDetails recordDetails, long ticks)
    {
        await Database.ExecuteAsync(
            "INSERT INTO records (steam_id, mapname, route, ticks, name, velocity_start_xy, velocity_start_z, velocity_start_xyz, velocity_end_xy, velocity_end_z, velocity_end_xyz, checkpoints, style) " +
            "VALUES (@steamId, @mapName, @route, @ticks, @name, @velocityStartXY, @velocityStartZ, @velocityStartXYZ, @velocityEndXY, @velocityEndZ, @velocityEndXYZ, CAST(@checkpoints as jsonb), @style) " +
            "ON CONFLICT (steam_id, mapname, route, style) " +
            "DO UPDATE SET " +
            "ticks = EXCLUDED.ticks, " +
            "velocity_start_xy = EXCLUDED.velocity_start_xy, " +
            "velocity_start_z = EXCLUDED.velocity_start_z, " +
            "velocity_start_xyz = EXCLUDED.velocity_start_xyz, " +
            "velocity_end_xy = EXCLUDED.velocity_end_xy, " +
            "velocity_end_z = EXCLUDED.velocity_end_z, " +
            "velocity_end_xyz = EXCLUDED.velocity_end_xyz, " +
            "checkpoints = EXCLUDED.checkpoints, " +
            "style = EXCLUDED.style " +
            "WHERE EXCLUDED.ticks < records.ticks",
            new
            {
                steamId = recordDetails.SteamId,
                mapName = recordDetails.MapName,
                route = recordDetails.Route,
                ticks,
                name = recordDetails.Name,
                velocityStartXY = recordDetails.Timer.VelocityStartXY,
                velocityStartZ = recordDetails.Timer.VelocityStartZ,
                velocityStartXYZ = recordDetails.Timer.VelocityStartXYZ,
                velocityEndXY = recordDetails.Timer.VelocityEndXY,
                velocityEndZ = recordDetails.Timer.VelocityEndZ,
                velocityEndXYZ = recordDetails.Timer.VelocityEndXYZ,
                checkpoints = JsonSerializer.Serialize(recordDetails.Timer.Checkpoints),
                style = recordDetails.Style
            });
    }

    private async Task SaveOrUpdateReplay(byte[] replayBytes, string steamId, string mapName, string route, long ticks, string style)
    {
        if (replayBytes.Length > 0)
        {
            Console.WriteLine("SAVING REPLAY: " + replayBytes.Length + " bytes " + mapName + " " + route + " " + ticks +
                              " " + style);
            
            var gameReplay = Replay.Deserialize(replayBytes);
            var url = await SaveReplayToS3(gameReplay, mapName, route, steamId, style);
            
            
            var oldReplay = (await Database.QueryAsync<V_Replay>(
                "SELECT * FROM v_replays WHERE steam_id = @steamId and mapname = @mapName and route = @route and style = @style",
                new { steamId, mapName, route, style })).FirstOrDefault();

            if (oldReplay != null)
            {
                await Database.ExecuteAsync(
                    "UPDATE replays SET ticks = @ticks, replay_url = @replay_url WHERE id = @id",
                    new { ticks, replay_url = url, id = oldReplay.Id });
                await DeleteReplayFromS3(oldReplay.ReplayUrl);
            }
            else
            {
                await Database.ExecuteAsync(
                    "INSERT INTO replays (steam_id, mapname, route, ticks, replay_url, style) VALUES (@steamId, @mapName, @route, @ticks, @replay_url, @style)",
                    new { steamId, mapName, route, ticks, replay_url = url, style });
            }

        }
        else
        {
            Console.WriteLine("Replay bytes is 0, failing to save replay");
        }
    }

    private async Task SavePlayerRecord(RecordDetails recordDetails)
    {
        try
        {
            var ticks = recordDetails.Timer.TimerTicks;

            var oldWorldRecord = await GetWorldRecord(recordDetails.MapName, recordDetails.Route, recordDetails.Style);
            var oldRecord = await GetPersonalRecord(recordDetails.MapName, recordDetails.Route, recordDetails.SteamId, recordDetails.Style);
            
            await InsertOrUpdateRecord(recordDetails, ticks);

            var newRecord = await GetPersonalRecord(recordDetails.MapName, recordDetails.Route, recordDetails.SteamId, recordDetails.Style);

            var isStage = recordDetails.Route.StartsWith("s");
            
            if (newRecord == null)
            {
                Console.WriteLine("newRecord is null");
                return;
            }

            var noPreviousWorldRecord = oldWorldRecord == null;
            var beatPreviousWorldRecord = oldWorldRecord != null && ticks < oldWorldRecord.Ticks;
            var betWorldRecord = noPreviousWorldRecord || beatPreviousWorldRecord;
            
            var noPreviousRecord = oldRecord == null;
            var beatPreviousRecord = oldRecord != null && ticks < oldRecord.Ticks;
            var betPersonalRecord = noPreviousRecord || beatPreviousRecord;
            
            var isTop100 = newRecord.Position <= 100;

            
            
            var mapZone = EntityManager.FindEntity<Map>().GetComponent<MapZone>();
            await mapZone.LoadRecords(Database, recordDetails.MapName);

            // update player record (this could be optimized to only update if they beat their previous record)
            var player = EntityManager.FindEntity<SurfPlayer>(recordDetails.PlayerSlot.ToString());
            var playerInfo = player.GetComponent<PlayerComponent>();
            if (playerInfo != null)
            {
                playerInfo.InMemoryUpdatePR(newRecord, recordDetails.Route, recordDetails.Style);
            }

            var previousPoints = playerInfo.StylePoints;
            // update player points
            await playerInfo.LoadStats(Database);
            var newPoints = playerInfo.StylePoints;
            
            
            Server.NextFrame(() =>
            {
                // reget player after async
                player = EntityManager.FindEntity<SurfPlayer>(recordDetails.PlayerSlot.ToString());
                var actualPlayer = player.GetPlayer();
                
                var prefix = recordDetails.Route == "main" ? "" : $" {CC.White}[{CC.Secondary}{recordDetails.Route}{CC.White}]";
                var stylePrefix = recordDetails.Style == "normal" ? "" : $" {CC.White}[{CC.Secondary}{recordDetails.ToStyleDisplayString()}{CC.White}]";
                prefix += stylePrefix;

                var messageOne =
                    $" {CC.Main}{recordDetails.Name}{CC.White} finished in {CC.Main}{Utils.FormatTime(ticks)}";

                if (oldWorldRecord != null && oldWorldRecord.Ticks != 0)
                {
                    var wrDiff = ticks - oldWorldRecord.Ticks;
                    var wrDiffString = Utils.FormatTime(wrDiff);
                    if (wrDiff > 0)
                        messageOne += $" {CC.White}[{CC.Secondary}WR +{wrDiffString}{CC.White}]";
                    else if (wrDiff <= 0)
                        messageOne += $" {CC.White}[{CC.Secondary}WR -{wrDiffString}{CC.White}]";
                }

                
                var rankString =
                    $" {CC.Secondary}{newRecord.Position}{CC.White}/{CC.Secondary}{newRecord.TotalRecords}{CC.White}";
                var pointsString = $" {CC.Secondary}{newRecord.Points}{CC.White} points ({CC.Secondary}{playerInfo.StylePoints}{CC.White} total)";

                if (oldRecord == null)
                {
                    // Will is now rank 1/123
                    var newPlayerMessage = $" {CC.Main}{recordDetails.Name}{CC.White} is now rank {rankString} gaining {pointsString}";
                    if (isStage)
                    {
                        if (newRecord.Position < 10)
                        {
                            Server.PrintToChatAll(prefix + messageOne);
                            Server.PrintToChatAll(prefix + newPlayerMessage);
                        }
                        else
                        {
                            actualPlayer.PrintToChat(prefix + messageOne);
                            actualPlayer.PrintToChat(prefix + newPlayerMessage);
                        }
                    }
                    else
                    {
                        Server.PrintToChatAll(prefix + messageOne);
                        Server.PrintToChatAll(prefix + newPlayerMessage);
                    }
                    
                   
                }
                else if (ticks < oldRecord.Ticks)
                {
                    // Will improved with [-00:00:00.000] Rank 1/123
                    var improvementTime = oldRecord.Ticks - ticks;
                    var improvedPlayerMessage =
                        $" {CC.Main}{recordDetails.Name}{CC.White} improved with [{CC.Main}-{Utils.FormatTime(improvementTime)}{CC.White}] Rank {rankString}";
                    if (oldRecord.Points < newRecord.Points)
                    {
                        improvedPlayerMessage +=
                            $" gaining {CC.Secondary}{newRecord.Points - oldRecord.Points}{CC.White} points";
                    }

                    if (isStage)
                    {
                        if (newRecord.Position < 10)
                        {
                            Server.PrintToChatAll(prefix + messageOne);
                            Server.PrintToChatAll(prefix + improvedPlayerMessage);
                        }
                        else
                        {
                            actualPlayer.PrintToChat(prefix + messageOne);
                            actualPlayer.PrintToChat(prefix + improvedPlayerMessage);
                        }
                    }
                    else
                    {
                        Server.PrintToChatAll(prefix + messageOne);
                        Server.PrintToChatAll(prefix + improvedPlayerMessage);
                    }
                }
                else
                {
                    var missedTime = ticks - oldRecord.Ticks;
                    // Will missed their best time by [+00:00:00.000] Rank 1/123
                    var worsePlayerMessage =
                        $" {CC.Main}{recordDetails.Name}{CC.White} missed their best time by [{CC.Main}+{Utils.FormatTime(missedTime)}{CC.White}] Rank {rankString}";
                    if (isStage && recordDetails.IsSecondary)
                    {
                        // do nothing
                        
                        // actualPlayer.PrintToChat(prefix + messageOne);
                        // actualPlayer.PrintToChat(prefix + worsePlayerMessage);
                    } else if (isStage && !recordDetails.IsSecondary)
                    {
                        actualPlayer.PrintToChat(prefix + messageOne);
                        actualPlayer.PrintToChat(prefix + worsePlayerMessage);
                    }
                    else
                    {
                        Server.PrintToChatAll(prefix + messageOne);
                        Server.PrintToChatAll(prefix + worsePlayerMessage);
                    }
                }

                // probably dont need new poitns check?
                if (recordDetails.Style == "normal" && previousPoints == 0 && newPoints != 0)
                {
                    Server.PrintToChatAll($" {CC.Main}{recordDetails.Name}{CC.White} unlocked an achievement: {CC.Secondary}First Points!");
                    Server.PrintToChatAll($" {CC.White} Way to go! Keep up the fantastic surfing! {CC.Main}(⌐■_■)");
                }


                if (betWorldRecord)
                {
                    var newWorldRecordMessage =
                        $" {CC.Main}{recordDetails.Name}{CC.White} set a new {CC.Secondary}WR {CC.White}with {CC.Main}{Utils.FormatTime(ticks)}! {CC.White}(⌐■_■)";
                    Server.PrintToChatAll(prefix + newWorldRecordMessage);
                }
            });
            

            if (!betPersonalRecord)
            {
                return;
            }
            
            // Update everyone elses position
            var allPlayers = EntityManager.Entities<SurfPlayer>();
            foreach (var surfPlayer in allPlayers)
            {
                // if this is the player who just finished, skip
                if (surfPlayer.Id == player.Id) continue;
                var surfPlayerInfo = surfPlayer.GetComponent<PlayerComponent>();
                if (surfPlayerInfo == null) continue;
                // if this player has a better time than the player who just finished, increment their position
                if (!surfPlayerInfo.PlayerRecords.ContainsKey(recordDetails.Route)) continue;
                if (!surfPlayerInfo.PlayerRecords[recordDetails.Route].ContainsKey(recordDetails.Style)) continue;
                
                if (noPreviousRecord)
                {
                    surfPlayerInfo.PlayerRecords[recordDetails.Route][recordDetails.Style].Position++;
                }
                
                var oldPosition = oldRecord?.Position ?? Int32.MaxValue;
                var newPosition = newRecord.Position;
                
                var thisPlayerPosition = surfPlayerInfo.PlayerRecords[recordDetails.Route][recordDetails.Style].Position;

                if (thisPlayerPosition >= newPosition && thisPlayerPosition < oldPosition)
                {
                    surfPlayerInfo.PlayerRecords[recordDetails.Route][recordDetails.Style].Position++;
                }
            }
            
            // Replay logic.
            // First thing is if you didn't beat a PR we aint saving.
            if (betPersonalRecord)
            {
                // Now if you are VIP we save regardless of top100
                if (recordDetails.IsVip)
                {
                    await SaveOrUpdateReplay(recordDetails.ReplayBytes, recordDetails.SteamId,
                        recordDetails.MapName,
                        recordDetails.Route, ticks, recordDetails.Style);

                } else if (isTop100)
                    // if you are top100 this will also encapsulate WR
                {
                    await SaveOrUpdateReplay(recordDetails.ReplayBytes, recordDetails.SteamId, recordDetails.MapName,
                        recordDetails.Route, ticks, recordDetails.Style);
                }
            }
            
            Server.NextFrame(() =>
            {
                if (betWorldRecord)
                {
                    // todo: these should probably go in db ie hostname 
                    var hostname = ConVar.Find("hostname")!.StringValue;
                    var wrEvent = new EventPlayerWR
                    {
                        MapName = recordDetails.MapName,
                        RouteKey = recordDetails.Route,
                        RouteName = recordDetails.MapRoute.Name,
                        PlayerName = recordDetails.Name,
                        Ticks = ticks,
                        Hostname = hostname,
                        MapTier = recordDetails.MapRoute.Tier.ToString(),
                        Style = recordDetails.Style
                    };
                    if (oldWorldRecord != null)
                    {
                        wrEvent.Diff = ticks - oldWorldRecord.Ticks;
                    }

                    EventManager.Publish(wrEvent);
                }
            });
            
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }
}