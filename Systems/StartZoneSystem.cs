using System.Text;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace WST;

public class StartZoneSystem : System
{
    public StartZoneSystem(EventManager eventManager, EntityManager entityManager, Database database)
        : base(eventManager, entityManager, database)
    {
        EventManager.Subscribe<OnStartTouchEvent>(OnStartTouch);
        EventManager.Subscribe<OnEndTouchEvent>(OnEndTouch);
    }
    

    public void OnStartTouch(OnStartTouchEvent e)
    {
        Entity entity = EntityManager.FindEntityOrThrow<SurfPlayer>(e.PlayerSlot.ToString());
        var playerInfo = entity.GetComponent<PlayerComponent>();
        if (playerInfo == null) return;
        
        var map = EntityManager.FindEntity<Map>();
        var mapZones = map.GetComponent<MapZone>();
        if (mapZones == null) return;
        var route = mapZones.GetRoute(playerInfo.RouteKey);
        if (route == null) return;

        if (route.Start.TargetName == e.TriggerName)
        {
            // entering startzone
            entity.RemoveComponent<PlayerTimerComponent>();
            if (playerInfo.Style == "prac")
            {
                playerInfo.Style = "normal";
            }
        }
        
        // subroute
        if (playerInfo.RouteKey != "main") return;
        
        var stageRoutes = mapZones.GetStageRoutes();
        var stageRoute = stageRoutes.FirstOrDefault(x => x.Start.TargetName == e.TriggerName);
        if (stageRoute == null) return;
        
        playerInfo.ChangeSecondaryRoute(stageRoute.Key);
    }

    public void OnEndTouch(OnEndTouchEvent e)
    {
        Console.WriteLine("ENDTOUCH");
        Entity entity = EntityManager.FindEntityOrThrow<SurfPlayer>(e.PlayerSlot.ToString());
        var playerInfo = entity.GetComponent<PlayerComponent>();
        if (playerInfo == null) return;
        if (playerInfo.Teleporting) return;

        var player = playerInfo.GetPlayer();
        
        var replay = entity.GetComponent<PlayerReplayComponent>();
        if (replay != null)
        {
            return;
        }
        
        var map = EntityManager.FindEntity<Map>();
        var mapZones = map.GetComponent<MapZone>();
        if (mapZones == null) return;
        var route = mapZones.GetRoute(playerInfo.RouteKey);
        if (route == null) return;

        if (route.Start.TargetName == e.TriggerName)
        {
             // Standard we are starting a run
            entity.RemoveComponent<PlayerTimerComponent>();
        
            var timerComponent = new PlayerTimerComponent();
            var timer = timerComponent.Primary;
            var startVelocity = new Vector(player.PlayerPawn.Value.AbsVelocity.X, player.PlayerPawn.Value.AbsVelocity.Y, player.PlayerPawn.Value.AbsVelocity.Z);
            timer.VelocityStartXY = startVelocity.Length2D();
            timer.VelocityStartZ = startVelocity.Z;
            timer.VelocityStartXYZ = startVelocity.Length();
            timer.RouteKey = playerInfo.RouteKey;

            if (timer.VelocityStartXY > route.VelocityCapStartXY)
            {
                Utils.AdjustPlayerVelocity(player, 270);
            }
        
            entity.AddComponent(timerComponent);
        
            // Round to a whole number
            var velStartDisplay = Math.Round(timer.VelocityStartXYZ, 0);

            var chatString = new StringBuilder();
            chatString.Append($" {CC.Secondary}⚡ {CC.White}Start: {CC.Secondary}{velStartDisplay} u/s");

            if (playerInfo.PR != null)
            {
                var diff = Math.Round(timer.VelocityStartXYZ - playerInfo.PR.VelocityStartXYZ, 0);
                if (diff > 0)
                    chatString.Append($" {CC.White}| PR: {CC.Secondary}+{diff} u/s");
                else if (diff < 0)
                    chatString.Append($" {CC.White}| PR: {CC.Secondary}{diff} u/s");
            }
            var mapWr = mapZones.GetWorldRecord(playerInfo.RouteKey, playerInfo.Style);
            if (mapWr != null)
            {
                var diff = Math.Round(timer.VelocityStartXYZ - mapWr.VelocityStartXYZ, 0);
                if (diff > 0)
                    chatString.Append($" {CC.White}| WR: {CC.Secondary}+{diff} u/s");
                else if (diff < 0)
                    chatString.Append($" {CC.White}| WR: {CC.Secondary}{diff} u/s");
            }
        
        
            player.PrintToChat(chatString.ToString());
            
        }
        
        // Ok we are LEAVING A ZONE. But is it potentially a (subroute)?
        // So for this to be a stage the following must be true.
            
        // 1. We are in the main route.
        // 2. Our timer is going
        // 3. The zone we just left is also the start zone of a stage route
            
        if (playerInfo.RouteKey != "main") return;
        var playerTimer = entity.GetComponent<PlayerTimerComponent>();
        if (playerTimer == null) return;
        var stageRoutes = mapZones.GetStageRoutes();
        var stageRoute = stageRoutes.FirstOrDefault(x => x.Start.TargetName == e.TriggerName);
        if (stageRoute == null) return;
        
        // Ok now we also know we are doing a stage route.
        // We have to now check if we are going below the stage route's velocity cap.
        // If so we can start the subroute timer
        
        // This is purely visual (pinfo.SecondaryRoute), its so we can show the correct route in the hud while still in the 
        // startzone or even if we didnt start the timer.
        playerInfo.ChangeSecondaryRoute(stageRoute.Key);

        var velocity = new Vector(player.PlayerPawn.Value.AbsVelocity.X, player.PlayerPawn.Value.AbsVelocity.Y, player.PlayerPawn.Value.AbsVelocity.Z);
        var velocityXY = velocity.Length2D();
        if (velocityXY < stageRoute.VelocityCapStartXY)
        {
            // Ok we are going slow enough to start the subroute timer.
            var secondaryTimer = new TimerData
            {
                VelocityStartXY = velocityXY,
                VelocityStartZ = velocity.Z,
                VelocityStartXYZ = velocity.Length(),
                RouteKey = stageRoute.Key
            };
            playerTimer.Secondary = secondaryTimer;
        }
    }
}