using CounterStrikeSharp.API.Modules.Utils;

namespace WST;

public class ReplayRecorderSystem : System
{
    public ReplayRecorderSystem(EventManager eventManager, EntityManager entityManager, Database database)
        : base(eventManager, entityManager, database)
    {
        eventManager.Subscribe<OnTickEvent>(Update);
    }

    private void Update(OnTickEvent e)
    {
        var entities = EntityManager.Entities<SurfPlayer>();
        foreach (var entity in entities)
        {
            var playerComponent = entity.GetComponent<PlayerComponent>();
            if (playerComponent == null) continue;
            var player = playerComponent.GetPlayer();
            if (!player.NativeIsValidAliveAndNotABot()) continue;
            var timer = entity.GetComponent<PlayerTimerComponent>();
            if (timer == null) continue;
            if (!player.Pawn.IsValid) continue;
            if (!player.PlayerPawn.IsValid) continue;

            var playerPos = player.Pawn.Value.AbsOrigin;
            var playerAngle = player.PlayerPawn.Value.EyeAngles;
            var buttons = player.Pawn.Value.MovementServices.Buttons.ButtonStates[0];
            var flags = player.Pawn.Value.Flags;
            var moveType = player.Pawn.Value.MoveType;

            var frame = new Replay.FrameT
            {
                Pos = new ZoneVector(playerPos.X, playerPos.Y, playerPos.Z),
                Ang = new ZoneVector(playerAngle.X, playerAngle.Y, playerAngle.Z),
                Buttons = buttons,
                Flags = flags,
                MoveType = moveType
            };

            timer.Primary.Recording.Frames.Add(frame);
            if (timer.Secondary != null)
            {
                timer.Secondary.Recording.Frames.Add(frame);
            }
        }
    }
}