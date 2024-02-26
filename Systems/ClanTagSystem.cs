namespace WST;

public class UpdateClanTagEvent
{
    public int PlayerSlot { get; set; }
}
public class ClanTagSystem : System
{
    public ClanTagSystem(EventManager eventManager, EntityManager entityManager, Database database)
        : base(eventManager, entityManager, database)
    {
        EventManager.Subscribe<UpdateClanTagEvent>(UpdateClanTag);
    }

    private void UpdateClanTag(UpdateClanTagEvent e)
    {
        var entity = EntityManager.FindEntity<SurfPlayer>(e.PlayerSlot.ToString());
        var playerInfo = entity.GetComponent<PlayerComponent>();
        if (playerInfo == null) return;
       
        var player = playerInfo.GetPlayer();

        if (player == null || !player.IsValid || player.IsBot || player.IsHLTV || player.AuthorizedSteamID == null) return;

        player.Clan = $"[#{playerInfo.StyleRank}] {playerInfo.ChatRank()}";
    }
}