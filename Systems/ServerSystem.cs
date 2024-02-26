using System.Text.Json;
using CounterStrikeSharp.API;

namespace WST;

public class OnServerStatusUpdateEvent
{
    public string ServerId { get; set; }
}

public class PlayerJson
{
    public string Name { get; set; }
    public string RouteKey { get; set; }
    public V_Player PlayerStats { get; set; }
}

public class ServerSystem : System
{
    public ServerSystem(EventManager eventManager, EntityManager entityManager, Database database)
        : base(eventManager, entityManager, database)
    {
        EventManager.Subscribe<OnServerStatusUpdateEvent>(OnServerStatusUpdate);
    }
    
    private void OnServerStatusUpdate(OnServerStatusUpdateEvent e)
    {
        var surfPlayers = EntityManager.Entities<SurfPlayer>();
        var map = Server.MapName;
        var maxPlayers = Server.MaxPlayers;

        var playerComponents = new List<PlayerJson>();
        foreach (var surfPlayer in surfPlayers)
        {
            var playerInfo = surfPlayer.GetComponent<PlayerComponent>();
            if (playerInfo == null) continue;
            
            var playerJson = new PlayerJson
            {
                Name = playerInfo.Name,
                RouteKey = playerInfo.RouteKey,
                PlayerStats = playerInfo.PlayerStats
            };
            playerComponents.Add(playerJson);
        }

        var playerCount = playerComponents.Count;
        

        Task.Run(async () =>
        {
            try
            {
                await Database.ExecuteAsync("update servers " +
                                            "set current_map = @current_map, " +
                                            "players = CAST(@players as jsonb), " +
                                            "player_count = @player_count, " +
                                            "total_players = @total_players " +
                                            "where server_id = @id", new
                {
                    id = e.ServerId,
                    current_map = map,
                    player_count = playerCount,
                    total_players = maxPlayers,
                    players = JsonSerializer.Serialize(playerComponents, new JsonSerializerOptions
                    {
                        DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    })
                });
            } 
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        });
    }
}