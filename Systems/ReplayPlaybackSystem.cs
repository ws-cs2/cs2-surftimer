using System.Text.Json;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using WST.Game;

namespace WST;

internal class LoadReplayEvent
{
    public string MapName { get; set; }
    public int PlayerSlot { get; set; }
    public int Position { get; set; }
}


public class EventOnLoadBot
{
    public string MapName { get; set; }
    public string Route { get; set; }
    public string Style { get; set; }
}

public class ReplayPlaybackSystem : System
{
    public const int FL_ONGROUND = 1 << 0;
    public const int FL_DUCKING = 1 << 1;

    private Replay? _replay;

    public ReplayPlaybackSystem(EventManager eventManager, EntityManager entityManager, Database database)
        : base(eventManager, entityManager, database)
    {
        EventManager.Subscribe<OnTickEvent>(Update);
        EventManager.SubscribeAsync<LoadReplayEvent>(OnLoadReplay);
        EventManager.Subscribe<EventPlayerWR>(OnNewWr);
        EventManager.Subscribe<EventOnLoadBot>(OnLoadBot);
    }

    private void OnLoadBot(EventOnLoadBot bot)
    {
        OnLoadReplayBot(bot.MapName, "main", bot.Style);
    }
    private void OnNewWr(EventPlayerWR wr)
    {
        if (wr.RouteKey != "main")
        {
            return;
        }

        if (wr.Style != Wst.Instance._currentGameServer.Style)
        {
            return;
        }
        
        OnLoadReplayBot(wr.MapName, "main", wr.Style);
    }

    private async Task OnLoadReplayBot(string mapName, string route, string style)
    {
        try
        {
            var firstRow = await V_Replay.GetRecordReplay(Database, mapName, route, 1, style);
            Server.NextFrame(() =>
            {
                if (firstRow == null)
                {
                    Server.PrintToChatAll($" {ChatColors.Gold}No replay found for {ChatColors.White}{mapName}");
                    return;
                }

                var replayDataBytes = firstRow.Replay;
                var replay = Replay.Deserialize(replayDataBytes);

                var replayComponent = new PlayerReplayComponent(replay, 0, firstRow.Route, firstRow.PlayerName, firstRow.Style, firstRow.CustomHudUrl);

                var entities = EntityManager.Entities<SurfBot>();
                if (entities.Count == 0)
                {
                    Console.WriteLine("[OnLoadReplayBot] Could not find bot entity");
                    return;
                }
                if (entities.Count > 1)
                {
                    Console.WriteLine("[OnLoadReplayBot] Found more than one bot entity");
                    return;
                }
                
                var entity = entities[0];
                
                entity.AddComponent(replayComponent);

                var player = entity.GetPlayer();
                if (player == null || !player.IsValid)
                {
                    Console.WriteLine("[OnLoadReplayBot] Could not find bot player");
                    return;
                }
                var playerNameSchema = new SchemaString<CBasePlayerController>(player, "m_iszPlayerName");
                if (playerNameSchema == null)
                {
                    Console.WriteLine("[OnLoadReplayBot] Could not find bot player name schema");
                    return;
                }
                playerNameSchema.Set($"[WR] {firstRow.PlayerName} - {Utils.FormatTime(firstRow.Ticks)}");
                Utilities.SetStateChanged(player, "CBasePlayerController", "m_iszPlayerName");

            });
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    private async Task OnLoadReplay(LoadReplayEvent e)
    {
        var player = EntityManager.FindEntity<SurfPlayer>(e.PlayerSlot.ToString());
        player.RemoveComponent<PlayerTimerComponent>();
        
        var playerInfo = player.GetComponent<PlayerComponent>();
        var route = playerInfo.RouteKey;

        // shitty ass coding why am am i naming shit like this
        V_Replay? firstRow = null;
        // stupid hack -1 means self
        if (e.Position == -1)
        {
            firstRow = await V_Replay.GetReplay(Database, e.MapName, route, playerInfo.SteamId, playerInfo.Style);
        }
        else
        {
            firstRow = await V_Replay.GetRecordReplay(Database, e.MapName, route, e.Position, playerInfo.Style);
        }
        if (firstRow == null || firstRow.Replay == null)
        {
            Server.NextFrame(() =>
            {
                var playerController = playerInfo.GetPlayer();
                playerController.PrintToChat($" {CC.White}No {CC.Main}replay {CC.White}found for {CC.Secondary}{route} {CC.Main}on {CC.Secondary}{e.MapName}");
            });
            return;
        }
        
        var wrName = firstRow.PlayerName;
        var wrTicks = firstRow.Ticks;
        var customHudUrl = firstRow.CustomHudUrl;

        Server.NextFrame(() =>
        {
            var replayDataBytes = firstRow.Replay;
            var replay = Replay.Deserialize(replayDataBytes);
            
            Console.WriteLine("Replay Frames: " + replay.Frames.Count);

            if (replay.Frames.Count == 0)
            {
                Console.WriteLine("Replay Frames is 0, deleting replay");
                firstRow.Delete(Database);
                playerInfo.GetPlayer().PrintToChat($" {ChatColors.Red}Replay Error: ${CC.White} Attempting to clean up, please try !replay again.");
                return;
            }
            
            var desiredPosition = e.Position;
            var replayPosition = firstRow.Position;
            
            if (desiredPosition != -1 && desiredPosition != replayPosition)
            {
                playerInfo.GetPlayer().PrintToChat($" {CC.Main}[oce.surf] {CC.White}No replay found for position {CC.Secondary}{desiredPosition}/{firstRow.TotalRecords}{CC.White}, playing closest replay at position {CC.Secondary}{replayPosition}/{firstRow.TotalRecords}");
            }
            
            playerInfo.GetPlayer().PrintToChat($" {CC.Main}[oce.surf] {CC.White}Type !main or !r to exit replay mode | Player: {CC.Secondary}{firstRow.PlayerName}{CC.White} | {CC.Main}{Utils.FormatTime(firstRow.Ticks)} {CC.White}({CC.Secondary}{replayPosition}/{firstRow.TotalRecords}{CC.White})");

            // try
            // {
            //     List<double> velocities = new List<double>();
            //     for (int i = 1; i < replay.Frames.Count; i++)
            //     {
            //         Vector displacement = replay.Frames[i].Pos - replay.Frames[i - 1].Pos;
            //     
            //         double timeInterval = 1.0 / 64; // Time per tick
            //     
            //         Console.WriteLine(timeInterval);
            //     
            //         var velocityX = displacement.X / timeInterval;
            //         var velocityY = displacement.Y / timeInterval;
            //         var velocityZ = displacement.Z / timeInterval;
            //     
            //         var velocityXY = Math.Sqrt(Math.Pow((double)velocityX, 2) + Math.Pow((double)velocityY, 2));
            //         velocities.Add(velocityXY);
            //
            //         // Vector velocity = displacement / timeInterval;
            //         // velocities.Add(velocity.Length2D());
            //     }
            //
            //     // Write velocities to file as json in C:\tmp\velocities.json
            //     var velocitiesJson = JsonSerializer.Serialize(velocities);
            //     File.WriteAllText(@"C:\tmp\velocities.json", velocitiesJson);
            // } catch (Exception ex)
            // {
            //     Console.WriteLine(ex);
            // }
            
            player.AddComponent(new PlayerReplayComponent(replay, 0, firstRow.Route, firstRow.PlayerName, firstRow.Style, firstRow.CustomHudUrl));
        });
    }


    private void Update(OnTickEvent e)
    {
        // bot OR player
        var entities = EntityManager.Entities<SurfEntity>();
        foreach (var entity in entities)
        {
            var playerController = entity.GetPlayer();
            var replayComponent = entity.GetComponent<PlayerReplayComponent>();

            if (replayComponent == null) continue;

            var currentFrame = replayComponent.ReplayPlayback.Frames[replayComponent.Tick];
            var currentPosition = playerController.PlayerPawn.Value.AbsOrigin!;
            

            // Calculate velocity
            var velocity = (currentFrame.Pos - currentPosition) * 64;

            var isOnGround = (currentFrame.Flags & FL_ONGROUND) != 0;
            var isDucking = (currentFrame.Flags & FL_DUCKING) != 0;

            if (isOnGround)
                playerController.PlayerPawn.Value.MoveType = MoveType_t.MOVETYPE_WALK;
            else
                playerController.PlayerPawn.Value.MoveType = MoveType_t.MOVETYPE_NOCLIP;

            // check if our current position is more than 200 units away from the replay position
            if (currentPosition.DistanceTo(currentFrame.Pos) > 300)
                playerController.PlayerPawn.Value.Teleport(currentFrame.Pos, currentFrame.Ang, new Vector(nint.Zero));
            else
                playerController.PlayerPawn.Value.Teleport(new Vector(nint.Zero), currentFrame.Ang, velocity);


            replayComponent.Tick++;
            if (replayComponent.Tick >= replayComponent.ReplayPlayback.Frames.Count) replayComponent.Tick = 0;
        }
    }
}