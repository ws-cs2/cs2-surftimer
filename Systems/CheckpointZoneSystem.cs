using System.Text;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Modules.Utils;

namespace WST;

public class CC
{
    public static char Main = ChatColors.Olive;
    public static char Secondary = ChatColors.Yellow;
    public static char White = ChatColors.White;
}

public class CheckpointZoneSystem : System
{
    public CheckpointZoneSystem(EventManager eventManager, EntityManager entityManager, Database database)
        : base(eventManager, entityManager, database)
    {
        EventManager.Subscribe<OnStartTouchEvent>(OnStartTouch);
    }

    public void OnStartTouch(OnStartTouchEvent e)
    {
        Entity entity = EntityManager.FindEntityOrThrow<SurfPlayer>(e.PlayerSlot.ToString());
        var playerInfo = entity.GetComponent<PlayerComponent>();
        if (playerInfo == null) return;
        var playerTimerComponent = entity.GetComponent<PlayerTimerComponent>();
        if (playerTimerComponent == null) return;
        
        var map = EntityManager.FindEntity<Map>();
        var mapZones = map.GetComponent<MapZone>();
        if (mapZones == null) return;
        var route = mapZones.GetRoute(playerInfo.RouteKey);
        if (route == null) return;
        
        var checkpointZone = route.Checkpoints.FirstOrDefault(cp => cp.TargetName == e.TriggerName);
        

        if (checkpointZone != null)
        {
            var playerTimer = playerTimerComponent.Primary;
            var idx = route.Checkpoints.IndexOf(checkpointZone);
            
            // don't add the same checkpoint twice
            if (playerTimer.Checkpoints.Count > idx)
            {
                return;
            }
            
            var player = playerInfo.GetPlayer();
            var velocity = new Vector(player.PlayerPawn.Value.AbsVelocity.X, player.PlayerPawn.Value.AbsVelocity.Y, player.PlayerPawn.Value.AbsVelocity.Z);

            var cp = new CheckpointTimerComponent
            {
                TimerTicks = playerTimer.TimerTicks,
                VelocityStartXY = velocity.Length2D(),
                VelocityStartZ = velocity.Z,
                VelocityStartXYZ = velocity.Length()
            };
            playerTimer.Checkpoints.Add(cp);

            var time = Utils.FormatTime(playerTimer.TimerTicks);
            
            
            var velStartDisplay = Math.Round(cp.VelocityStartXY, 0);

            var chatString = new StringBuilder();
            chatString.Append($" {CC.Secondary}⚡ {CC.White}CP [{CC.Secondary}{idx + 1}] {CC.Main}{time} {CC.White}({CC.Secondary}{velStartDisplay} u/s{CC.White})");

            if (playerInfo.PR != null)
            {
                var prcp = playerInfo.PR.CheckpointsObject[idx];
                if (prcp != null)
                {
                    var timeDiff = cp.TimerTicks - prcp.TimerTicks;
                    var timeDiffDisplay = Utils.FormatTime(timeDiff);
                    
                    if (timeDiff > 0)
                        chatString.Append($" {CC.White}| PR: {CC.Main}+{timeDiffDisplay}");
                    else if (timeDiff < 0)
                        chatString.Append($" {CC.White}| PR: {CC.Main}-{timeDiffDisplay}");

                    var diff = Math.Round(cp.VelocityStartXY - prcp.VelocityStartXY, 0);
                    if (diff > 0)
                        chatString.Append($" {CC.White}({CC.Secondary}+{diff} u/s{CC.White})");
                    else if (diff < 0)
                        chatString.Append($" {CC.White}({CC.Secondary}{diff} u/s{CC.White})");
                }
            }
            var mapWr = mapZones.GetWorldRecord(playerInfo.RouteKey, playerInfo.Style);
            if (mapWr != null)
            {
                var wrcp = mapWr.CheckpointsObject[idx];
                if (wrcp != null)
                {
                    var timeDiff = cp.TimerTicks - wrcp.TimerTicks;
                    var timeDiffDisplay = Utils.FormatTime(timeDiff);
                    
                    if (timeDiff > 0)
                        chatString.Append($" {CC.White}| WR: {CC.Main}+{timeDiffDisplay}");
                    else
                        chatString.Append($" {CC.White}| WR: {CC.Main}-{timeDiffDisplay}");

                    var diff = Math.Round(cp.VelocityStartXY - wrcp.VelocityStartXY, 0);
                    if (diff > 0)
                        chatString.Append($" {CC.White}({CC.Secondary}+{diff} u/s{CC.White})");
                    else
                        chatString.Append($" {CC.White}({CC.Secondary}{diff} u/s{CC.White})");
                    
                    // WR: +0.000 (+0 u/s)
                    // var centerChatString = "";
                    // if (timeDiff > 0)
                    //     centerChatString += $"WR: +{timeDiffDisplay}";
                    // else
                    //     centerChatString += $"WR: -{timeDiffDisplay}";
                    //
                    // if (diff > 0)
                    //     centerChatString += $" (+{diff} u/s)";
                    // else
                    //     centerChatString += $" ({diff} u/s)";
                    //
                    // player.PrintToCenter(centerChatString);
                }
                
                
            }
            
            
            player.PrintToChat(chatString.ToString());
        }
    }
}