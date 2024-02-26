using System.Text;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace WST;

public class HudSystem : System
{
    const string PRIMARY_COLOR = "#0079FF";
    const string SECONDARY_COLOR = "#00DFA2";
    const string TERTIARY_COLOR = "#F6FA70";
    const string QUATERNARY_COLOR = "#FF0060";
    const string WHITE = "#FFFFFF";
    
    public HudSystem(EventManager eventManager, EntityManager entityManager, Database database)
        : base(eventManager, entityManager, database)
    {
        EventManager.Subscribe<OnTickEvent>(Update);
    }
    
    private string FormatStyle(string style)
    {
        if (style == "normal")
        {
            return "";
        }

        if (style == "tm")
        {
            style = "turbo";
        }
        return $" ({style})";
    }

    
    private ReplayHudData BuildReplayHudData(CCSPlayerController player, PlayerReplayComponent replay)
    {
        var speed = Math.Round(player.PlayerPawn.Value.AbsVelocity.Length2D());
        return new ReplayHudData
        {
            Speed = speed,
            Ticks = replay.Tick,
            Route = replay.Route,
            ReplayPlayerName = TruncatePlayerName(replay.PlayerName),
            TotalTicks = replay.TotalTicks,
            CustomHudUrl = replay.CustomHudUrl,
            Buttons = replay.ReplayPlayback.Frames[replay.Tick].Buttons,
            Style = FormatStyle(replay.Style)
        };
    }

    private RegularHudData BuildRegularHudData(CCSPlayerController player, PlayerComponent playerInfo, MapZone mapZones, PlayerTimerComponent? timerComponent, bool isSpec)
    {
        var speed = Math.Round(player.PlayerPawn.Value.AbsVelocity.Length2D());
        var timer = timerComponent?.Primary;
        var hudData = new RegularHudData
        {
            Speed = speed,
            Ticks = timer?.TimerTicks ?? 0,
            Route = playerInfo.RouteKey,
            Style = FormatStyle(playerInfo.Style)
        };

        if (playerInfo.PR != null)
        {
            hudData.PRTicks = playerInfo.PR.Ticks;
            hudData.PRPosition = playerInfo.PR.Position;
            hudData.PRTotalRecords = playerInfo.PR.TotalRecords;
        }

        var wr = mapZones.GetWorldRecord(playerInfo.RouteKey, playerInfo.Style);
        
        if (wr != null)
        {
            hudData.WRTicks = wr.Ticks;
            hudData.WRName = TruncatePlayerName(wr.Name);
        }
        
        if (isSpec)
        {
            // always show custom hud in spec
            hudData.CustomHudUrl = playerInfo.PlayerStats.CustomHudUrl;
            hudData.Buttons = player.Buttons;
        }
        else
        {
            // not specing so only show if on
            if (playerInfo.ShowCustomHud)
            {
                hudData.CustomHudUrl = playerInfo.PlayerStats.CustomHudUrl;
            }

            if (playerInfo.ShowKeys)
            {
                hudData.Buttons = player.Buttons;
            }
        }
        
        var lastCp = timer?.Checkpoints.LastOrDefault();
        if (lastCp != null)
        {
            var lastCpIdx = timer.Checkpoints.IndexOf(lastCp);
            if (wr != null)
            {
                var wrCp = wr.CheckpointsObject[lastCpIdx];
                if (wrCp != null)
                {
                    var timeDiff = wrCp.TimerTicks - lastCp.TimerTicks;
                    hudData.CpTicksDiff = timeDiff;
                }
            }
        }

        if (playerInfo.SecondaryRouteKey != null)
        {
            hudData.SecondaryRoute = playerInfo.SecondaryRouteKey;
        }

        return hudData;
    }

    private string RenderReplayHud(ReplayHudData replayHudData)
    {
        var speedLine = GenerateSpeedLine(replayHudData.Speed);
        var timeLine = GenerateTimeLine(replayHudData.Ticks, TERTIARY_COLOR);
        var replayLine = $" <font class='fontSize-s' color='{WHITE}'>REPLAY</font> <br>";
        var routeLine = $" <font color='gray' class='fontSize-s'>{replayHudData.Route}";
        routeLine += replayHudData.Style;
        routeLine += $" | {replayHudData.ReplayPlayerName} | {Utils.FormatTime(replayHudData.TotalTicks)}</font> <br>";
        var customHud = replayHudData.CustomHudUrl != null ? $" <img src=\"{replayHudData.CustomHudUrl}\"> <br>" : "";
        var playerButtons = (PlayerButtons) replayHudData.Buttons;
        var buttonsLine = GenerateButtonsLine(playerButtons);
        return speedLine + timeLine + buttonsLine + routeLine + customHud;
    }

    private string RenderHud(RegularHudData hudData)
    {
        var speedLine = GenerateSpeedLine(hudData.Speed);
        var timeColor = hudData.Ticks == 0 ? QUATERNARY_COLOR : SECONDARY_COLOR;
        var timeLine = GenerateTimeLine(hudData.Ticks, timeColor);
        
        var routeLineString = hudData.Route;

        if (hudData.SecondaryRoute != null)
        {
            routeLineString += $" ({hudData.SecondaryRoute})";
        }
        
        routeLineString += hudData.Style;
        
        if (hudData.PRTicks.HasValue && hudData.PRPosition.HasValue && hudData.PRTotalRecords.HasValue)
        {
            routeLineString += $" | PR: {Utils.FormatTime(hudData.PRTicks.Value)} ({hudData.PRPosition.Value}/{hudData.PRTotalRecords.Value})";
        }
        
        if (hudData.WRTicks.HasValue && hudData.WRName != null)
        {
            routeLineString += $" | WR: {Utils.FormatTime(hudData.WRTicks.Value)}({hudData.WRName})";
        }

        var routeLine = $" <font color='gray' class='fontSize-s'>{routeLineString}</font> <br>";

        var customHud = hudData.CustomHudUrl != null ? $" <img src=\"{hudData.CustomHudUrl}\"> <br>" : "";
        
        var buttonsLine = hudData.Buttons.HasValue ? GenerateButtonsLine(hudData.Buttons.Value) : "";
        
        var cpLine = hudData.CpTicksDiff.HasValue ? $" <font color='gray' class='fontSize-s'>{Utils.FormatTimeWithPlusOrMinus(hudData.CpTicksDiff.Value)}</font> <br>" : "";
        
        return speedLine + timeLine + cpLine + buttonsLine + routeLine + customHud;
    }

    private string GenerateSpeedLine(double speed)
    {
        return $" <font class='fontSize-xl' color='{PRIMARY_COLOR}'>{speed}</font> <br>";
    }

    private string GenerateTimeLine(int ticks, string color)
    {
        return  $" <font class='fontSize-l' color='{color}'>{Utils.FormatTime(ticks)}</font> <br>";
    }

    private string GenerateButtonsLine(PlayerButtons playerButtons)
    {
        return  $" <font>{((playerButtons & PlayerButtons.Moveleft) != 0 ? "A" : "_")} " +
                $"{((playerButtons & PlayerButtons.Forward) != 0 ? "W" : "_")} " +
                $"{((playerButtons & PlayerButtons.Moveright) != 0 ? "D" : "_")} " +
                $"{((playerButtons & PlayerButtons.Back) != 0 ? "S" : "_")} " +
                $"{((playerButtons & PlayerButtons.Jump) != 0 ? "J" : "_")} " +
                $"{((playerButtons & PlayerButtons.Duck) != 0 ? "C" : "_")}</font> <br>";
    }
    
    private string TruncatePlayerName(string? name)
    {
        if (name == null) return "";
        var newName = name.Replace("<", "").Replace(">", "");
        // check string length 
        if (newName.Length <= 10) return newName;
        // truncate string
        return newName.Substring(0, 10);
    }

    private class ReplayHudData
    {
        public double Speed { get; set; }
        public int Ticks { get; set; }
        public int TotalTicks { get; set; }
        public string Route { get; set; }
        
        public string Style { get; set; }
        public string ReplayPlayerName { get; set; }
        public string? CustomHudUrl { get; set; }
        public ulong Buttons { get; set; }
    }

    private class RegularHudData
    {
        public double Speed { get; set; }
        public int Ticks { get; set; }
        public string Route { get; set; }
        
        public string SecondaryRoute { get; set; }
        public int? PRTicks { get; set; }
        public int? PRPosition { get; set; }
        public int? PRTotalRecords { get; set; }
        public int? WRTicks { get; set; }
        public string? WRName { get; set; }
        public string? CustomHudUrl { get; set; }
        public PlayerButtons? Buttons { get; set; }
        
        public int? CpTicksDiff { get; set; }
        
        public string Style { get; set; }
    }


    private void Update(OnTickEvent e)
    {
        var entities = EntityManager.Entities<SurfPlayer>();
        var map = EntityManager.FindEntity<Map>();
        if (map == null)
        {
            return;
        }
        var mapZones = map.GetComponent<MapZone>();

        foreach (var entity in entities)
        {
            var player = entity.GetPlayer();
            if (!player.NativeIsValidAliveAndNotABot()) continue;
            if (mapZones == null) continue;
            var playerInfo = entity.GetComponent<PlayerComponent>();
            if (playerInfo == null) continue;
            
            var replay = entity.GetComponent<PlayerReplayComponent>();
            if (replay != null)
            {
                var replayHudData = BuildReplayHudData(player, replay);
                var replayHud = RenderReplayHud(replayHudData);
                TimerPrintHtml(player, playerInfo, replayHud);
                continue;
            }
            
            
            var timer = entity.GetComponent<PlayerTimerComponent>();
            // note timer may be null and this is valid (startzones etc)
            // Todo: Shouldnt be in the hud lol
            if (timer != null)
            {
                timer.Primary.TimerTicks++;
                if (timer.Secondary != null)
                {
                    timer.Secondary.TimerTicks++;
                }
            }
            
            var hudData = BuildRegularHudData(player, playerInfo, mapZones, timer, false);
            var hud = RenderHud(hudData); 
            TimerPrintHtml(player, playerInfo, hud);
        }

        foreach (var entity in entities)
        {
            var playerInfo = entity.GetComponent<PlayerComponent>();
            var player = playerInfo.GetPlayer();
            if (!player.IsValid) continue;

            // dont show spec hud to alive players
            if (player.PawnIsAlive)
            {
                continue;
            }

            var obsPawnHandle = player.ObserverPawn;
            if (!obsPawnHandle.IsValid) continue;
            var obsPawn = obsPawnHandle.Value;
            var observices = obsPawn.ObserverServices;
            if (observices == null) continue;
            var obsTargetHandle = observices.ObserverTarget;
            if (!obsTargetHandle.IsValid) continue;
            var target = obsTargetHandle.Value;
            if (target == null || !target.IsValid) continue;
            if (target.DesignerName != "player") continue;
            var targetPlayer = new CCSPlayerController(new CCSPlayerPawn(target.Handle).Controller.Value.Handle);
            var targetSlot = targetPlayer.Slot;
            // important: Could be a bot.
            var targetEntity = EntityManager.FindEntity<SurfEntity>(targetSlot.ToString());
            if (targetEntity == null) continue;

            var targetPlayerReplay = targetEntity.GetComponent<PlayerReplayComponent>();
            if (targetPlayerReplay != null)
            {
                var replayHudData = BuildReplayHudData(targetPlayer, targetPlayerReplay);
                var replayHud = RenderReplayHud(replayHudData);
                TimerPrintHtml(player, playerInfo, replayHud);
                continue;
            }
            
            var targetPlayerInfo = targetEntity.GetComponent<PlayerComponent>();
            if (targetPlayerInfo == null) continue;
            
            var targetTimer = targetEntity.GetComponent<PlayerTimerComponent>();
            // note timer may be null and this is valid (startzones etc)
            var hudData = BuildRegularHudData(targetPlayer, targetPlayerInfo, mapZones, targetTimer, true);
            var hud = RenderHud(hudData);
            TimerPrintHtml(player, playerInfo, hud);
        }
    }
    
    public void TimerPrintHtml(CCSPlayerController player, PlayerComponent playerInfo, string hudContent)
    {
        if (!playerInfo.ShowHud)
        {
            return;
        }
        var @event = new EventShowSurvivalRespawnStatus(false)
        {
            LocToken = hudContent,
            Duration = 5,
            Userid = player
        };
        @event.FireEvent(false);
        @event = null;
    }
}