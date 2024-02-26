using CounterStrikeSharp.API;

namespace WST;

public class TurbomasterSystem : System
{
    public const int FL_ONGROUND = 1 << 0;
    public TurbomasterSystem(EventManager eventManager, EntityManager entityManager, Database database)
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
            if (playerInfo.Style != "tm") continue;
            if (!player.Pawn.IsValid) continue;
            var buttons = (PlayerButtons) player.Pawn.Value.MovementServices.Buttons.ButtonStates[0];
            var flags = player.Pawn.Value.Flags;
            var moveType = player.Pawn.Value.MoveType;
            
            // if on ground do nothing
            if ((flags & FL_ONGROUND) != 0) continue;
            
            
            // todo

            var attack = PlayerButtons.Attack;
            var attack2 = PlayerButtons.Attack2;
            
            // if ((buttons & PlayerButtons.Attack2) == 0 && timer.TmInSlowMo)
            // {
            //     timer.TmInSlowMo = false;
            //     player.PlayerPawn.Value.GravityScale = 1f;
            //     var currentSpeedXY = Math.Round(player.PlayerPawn.Value.AbsVelocity.Length2D());
            //     var currentSpeedZ = Math.Round(player.PlayerPawn.Value.AbsVelocity.Z);
            //     var targetSpeed = currentSpeedXY * 10;
            //     Utils.AdjustPlayerVelocity(player, Math.Min((float) targetSpeed, timer.TmXYSpeedBeforeSlowMo));
            //     player.PlayerPawn.Value.AbsVelocity.Z = (float) Math.Min(timer.TmZSpeedBeforeSlowMo, currentSpeedZ * 10);
            //     timer.TmXYSpeedBeforeSlowMo = 0;
            //     timer.TmZSpeedBeforeSlowMo = 0;
            //     continue;
            // }
            //
            // if ((buttons & PlayerButtons.Attack2) != 0 && !timer.TmInSlowMo)
            // {
            //     timer.TmInSlowMo = true;
            //     player.PlayerPawn.Value.GravityScale = 0.02f;
            //     var currentSpeedXY = Math.Round(player.PlayerPawn.Value.AbsVelocity.Length2D());
            //     var currentSpeedZ = Math.Round(player.PlayerPawn.Value.AbsVelocity.Z);
            //     timer.TmXYSpeedBeforeSlowMo = (float) currentSpeedXY;
            //     timer.TmZSpeedBeforeSlowMo = (float) currentSpeedZ;
            //     var targetSpeed = currentSpeedXY / 10;
            //     Utils.AdjustPlayerVelocity(player, (float) targetSpeed);
            //     player.PlayerPawn.Value.AbsVelocity.Z = (float) currentSpeedZ / 10;
            //     continue;
            // }

            if ((buttons & PlayerButtons.Attack2) != 0)
            {
                var currentSpeedZ = player.PlayerPawn.Value.AbsVelocity.Z;
                
                var targetSpeed = currentSpeedZ - 15;
                player.PlayerPawn.Value.AbsVelocity.Z = targetSpeed;
                continue;
            }

            if ((buttons & PlayerButtons.Attack) != 0)
            {
                var currentSpeedXY = Math.Round(player.PlayerPawn.Value.AbsVelocity.Length2D());
                if (currentSpeedXY < 10)
                {
                    continue;
                }
                
                var targetSpeed = currentSpeedXY + 10;
                Utils.AdjustPlayerVelocity(player, (float) targetSpeed);
                continue;
            }
        }
    }
}