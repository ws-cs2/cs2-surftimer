using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace WST;

public class HSWSurfSystem : System
{
    public const int FL_ONGROUND = 1 << 0;
    public HSWSurfSystem(EventManager eventManager, EntityManager entityManager, Database database)
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
            if (playerInfo.Style != "hsw") continue;
            if (!player.Pawn.IsValid) continue;
            var buttons = (PlayerButtons) player.Pawn.Value.MovementServices.Buttons.ButtonStates[0];
            var flags = player.Pawn.Value.Flags;
            var moveType = player.Pawn.Value.MoveType;

            var timer = entity.GetComponent<PlayerTimerComponent>();
            if (timer == null)
            {
                continue;
            };
            //
            // bool bForward = ((buttons & IN_FORWARD) > 0 && vel[0] >= 100.0);
            // bool bMoveLeft = ((buttons & IN_MOVELEFT) > 0 && vel[1] <= -100.0);
            // bool bBack = ((buttons & IN_BACK) > 0 && vel[0] <= -100.0);
            // bool bMoveRight = ((buttons & IN_MOVERIGHT) > 0 && vel[1] >= 100.0);
            // if (!g_bInStartZone[client] && !g_bInStageZone[client])
            // {
            //     if((bForward || bBack) && !(bMoveLeft || bMoveRight))
            //     {
            //         vel[0] = 0.0;
            //         buttons &= ~IN_FORWARD;
            //         buttons &= ~IN_BACK;
            //     }
            //     if((bMoveLeft || bMoveRight) && !(bForward || bBack))
            //     {
            //         vel[1] = 0.0;
            //         buttons &= ~IN_MOVELEFT;
            //         buttons &= ~IN_MOVERIGHT;
            //     }
            // }
            
            
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

            // if ((buttons & PlayerButtons.Moveleft) != 0)
            // {
            //     playerInfo.Style = "normal";
            //     player.PrintToChat($" Style set to {CC.Main}Normal {CC.Main}, A detected");
            // }
            //
            // if ((buttons & PlayerButtons.Moveright) != 0)
            // {
            //     playerInfo.Style = "normal";
            //     player.PrintToChat($" Style set to {CC.Main}Normal {CC.Main}, D detected");
            // }

            var bForward = ((buttons & PlayerButtons.Forward) > 0);
            var bMoveLeft = ((buttons & PlayerButtons.Moveleft) > 0);
            var bBack = ((buttons & PlayerButtons.Back) > 0);
            var bMoveRight = ((buttons & PlayerButtons.Moveright) > 0);
            

            if ((bForward || bBack) && !(bMoveLeft || bMoveRight))
            {
                player.PrintToChat($" Invalid keys detected for {CC.Main}HSW {CC.Main}, resetting");
                Wst.Instance.Restart(player);
                continue;
            }
            if ((bMoveLeft || bMoveRight) && !(bForward || bBack))
            {
                player.PrintToChat($" Invalid keys detected for {CC.Main}HSW {CC.Main}, resetting");
                Wst.Instance.Restart(player);
                continue;
            }
            
        }
    }
}