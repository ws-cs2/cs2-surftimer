using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace WST;

public class SidewaysSurfSystem : System
{
    public const int FL_ONGROUND = 1 << 0;
    public SidewaysSurfSystem(EventManager eventManager, EntityManager entityManager, Database database)
        : base(eventManager, entityManager, database)
    {
        EventManager.Subscribe<OnTickEvent>(OnTick);
    }
    
    private void OnTick(OnTickEvent e)
    {
        var entities = EntityManager.Entities<SurfPlayer>();

        foreach (var entity in entities)
        {
            var player = entity.GetPlayer();
            if (!player.NativeIsValidAliveAndNotABot()) continue;
            var playerInfo = entity.GetComponent<PlayerComponent>();
            if (playerInfo == null) continue;
            if (playerInfo.Style != "sw") continue;
            if (!player.Pawn.IsValid) continue;
            var buttons = (PlayerButtons) player.Pawn.Value.MovementServices.Buttons.ButtonStates[0];
            var flags = player.Pawn.Value.Flags;
            var moveType = player.Pawn.Value.MoveType;

            var timer = entity.GetComponent<PlayerTimerComponent>();
            if (timer == null)
            {
                continue;
            };
            
            // if on ground do nothing
            if ((flags & FL_ONGROUND) != 0) continue;
            // if on ladder do nothing
            if (moveType == MoveType_t.MOVETYPE_LADDER) continue;
            
            var currentSpeedXY = Math.Round(player.PlayerPawn.Value.AbsVelocity.Length2D());
            // some grace period
            if (currentSpeedXY < 250)
            {
                continue;
            }

            if ((buttons & PlayerButtons.Moveleft) != 0)
            {
                // playerInfo.Style = "normal";
                // player.PrintToChat($" Style set to {CC.Main}Normal {CC.Main}, A detected");
                player.PrintToChat($" Invalid keys detected for {CC.Main}SW {CC.Main}, resetting");
                Wst.Instance.Restart(player);
                continue;
            }
            
            if ((buttons & PlayerButtons.Moveright) != 0)
            {
                // playerInfo.Style = "normal";
                // player.PrintToChat($" Style set to {CC.Main}Normal {CC.Main}, D detected");
                player.PrintToChat($" Invalid keys detected for {CC.Main}SW {CC.Main}, resetting");
                Wst.Instance.Restart(player);
                continue;
            }

        }
    }
}