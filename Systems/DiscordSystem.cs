using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace WST;

// {
//     "content": "**New World Record** | surf_evo - Main",
//     "embeds": [
//     {
//         "color": null,
//         "fields": [
//         {
//             "name": "Player",
//             "value": "Will",
//             "inline": true
//         },
//         {
//             "name": "Time",
//             "value": "01:25:718 (-00:00:984)",
//             "inline": true
//         },
//         {
//             "name": "Map Tier",
//             "value": "1",
//             "inline": true
//         }
//         ],
//         "footer": {
//             "text": "Server: hostname"
//         },
//         "timestamp": "2024-01-02T11:00:00.000Z",
//         "image": {
//             "url": "https://raw.githubusercontent.com/Sayt123/SurfMapPics/Maps-and-bonuses/csgo/surf_mesa_aether.jpg"
//         }
//     }
//     ],
//     "attachments": []
// }

public class EventPlayerWR
{
    public string MapName { get; set; }
    public string RouteKey { get; set; }
    public string RouteName { get; set; }
    public string PlayerName { get; set; }
    public int Ticks { get; set; }
    public int? Diff { get; set; }
    public string Hostname { get; set; }
    public string MapTier { get; set; }
    
    public string Style { get; set; }
}

public class DiscordSystem : System
{
    private readonly HttpClient _httpClient;
    
    private string MAP_WEBHOOK = Environment.GetEnvironmentVariable("DISCORD_MAP_WEBHOOK")!;
    private string STAGE_WEBHOOK = Environment.GetEnvironmentVariable("DISCORD_STAGE_WEBHOOK")!;
    private string BONUS_WEBHOOK = Environment.GetEnvironmentVariable("DISCORD_BONUS_WEBHOOK")!;
    private string STYLE_WEBHOOK = Environment.GetEnvironmentVariable("DISCORD_STYLE_WEBHOOK")!;
      
    public DiscordSystem(EventManager eventManager, EntityManager entityManager, Database database)
        : base(eventManager, entityManager, database)
    {
        EventManager.SubscribeAsync<EventPlayerWR>(OnPlayerRecord);
        _httpClient = new HttpClient();
    }
    
    
    public async Task OnPlayerRecord(EventPlayerWR wr)
    {
        var mapName = wr.MapName;
        var routeKey = wr.RouteKey;
        var routeName = wr.RouteName;
        var playerName = wr.PlayerName;
        var style = wr.Style;

        
        var timeString = Utils.FormatTime(wr.Ticks);
        if (wr.Diff.HasValue)
        {
            timeString += $" (-{Utils.FormatTime(wr.Diff.Value)})";
        }
        var hostname = wr.Hostname;
        var mapTier = wr.MapTier;
        
        
        var mapImg = $"https://raw.githubusercontent.com/Sayt123/SurfMapPics/Maps-and-bonuses/csgo/{mapName}.jpg";
        // check if mapImg exists
        var mapImgExists = await _httpClient.GetAsync(mapImg);
        if (!mapImgExists.IsSuccessStatusCode)
        {
            mapImg = null;
        }
        
        var recordType = "Map";
        var webhook = MAP_WEBHOOK;
        if (routeKey == "main" || routeKey == "boost")
        {
            recordType = "Map";
            webhook = MAP_WEBHOOK;
        } else if (routeKey.StartsWith("b"))
        {
            recordType = "Bonus";
            webhook = BONUS_WEBHOOK;
        } else if (routeKey.StartsWith("s"))
        {
            recordType = "Stage";
            webhook = STAGE_WEBHOOK;
        }
        
        if (style != "normal")
        {
            // dont do stages for styles
            if (recordType == "Stage")
            {
                return;
            }

            if (style == "lg")
            {
                recordType = "Low Gravity";
            } else if (style == "tm")
            {
                recordType = "Turbo";
            }
            else if (style == "sw")
            {
                recordType = "Sideways";
            } 
            else if (style == "hsw")
            {
                recordType = "Half-Sideways";
            }
            else if (style == "prac")
            {
                recordType = "Practice Mode/TAS";
            }
            else
            {
                recordType = style;
            }

            webhook = STYLE_WEBHOOK;
        }

        var json = new JsonObject
        {
            ["content"] = $"**New {recordType} Record** | {mapName} - {routeName}",
            ["embeds"] = new JsonArray
            {
                new JsonObject
                {
                    ["color"] = null,
                    ["fields"] = new JsonArray
                    {
                        new JsonObject
                        {
                            ["name"] = "Player",
                            ["value"] = playerName,
                            ["inline"] = true
                        },
                        new JsonObject
                        {
                            ["name"] = "Time",
                            ["value"] = timeString,
                            ["inline"] = true
                        },
                        new JsonObject
                        {
                            ["name"] = "Map Tier",
                            ["value"] = mapTier,
                            ["inline"] = true
                        }
                    },
                    ["footer"] = new JsonObject
                    {
                        ["text"] = $"Server: {hostname}"
                    },
                    ["timestamp"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                    
                }
            },
            ["attachments"] = new JsonArray()
        };    
        
        if (mapImg != null)
        {
            ((JsonObject)json["embeds"][0])["image"] = new JsonObject
            {
                ["url"] = mapImg
            };
        }
        
        var content = new StringContent(json.ToString(), Encoding.UTF8, "application/json");
        await _httpClient.PostAsync(webhook, content);
        // [Caff] beat the map World Record on < surf_beyer > with time < 01:18:36 [-00:00:04] > in the q3 Server on < = CS:S = > ]:
        var serverMessage =
            $" [{CC.Secondary}{playerName}{CC.White}] beat the Server Record on {CC.Secondary}{mapName}{CC.White} - {CC.Secondary}{routeName}{CC.White}";
        if (style != "normal")
        {
            serverMessage += $" - {CC.Secondary}{recordType}{CC.White}";
        }
        
        serverMessage += $" with time {CC.Main}{timeString}{CC.White} in the {CC.Secondary}{hostname}{CC.White} Server";
        Wst.Instance._broadcast.Send("chat", new ChatBroadcast
        {
            Message =  Utils.TagsToColorNames(serverMessage)
        });
    }
}