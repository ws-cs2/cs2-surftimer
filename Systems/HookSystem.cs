using CounterStrikeSharp.API;

namespace WST;

public class HookSystem : System
{
    public const int FL_ONGROUND = 1 << 0;
    public HookSystem(EventManager eventManager, EntityManager entityManager, Database database)
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
            if (!player.Pawn.IsValid) continue;
            
            var buttons = (PlayerButtons) player.Pawn.Value.MovementServices.Buttons.ButtonStates[0];
            var flags = player.Pawn.Value.Flags;
            var moveType = player.Pawn.Value.MoveType;

            var timer = entity.GetComponent<PlayerTimerComponent>();
            if (timer != null)
            {
                entity.RemoveComponent<PlayerTimerComponent>();
            };
            // todo

            var isUse = PlayerButtons.Use;
            
            // todo need raycasting
        }
    }
}