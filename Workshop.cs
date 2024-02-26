using System.Text.Json;
using System.Text.Json.Serialization;
using CounterStrikeSharp.API;

namespace WST;


public partial class GetPublishedFileDetails
{
    [JsonPropertyName("response")]
    public GetPublishedFileDetailsResponse Response { get; set; }
}

public partial class GetPublishedFileDetailsResponse
{
    [JsonPropertyName("result")]
    public long Result { get; set; }

    [JsonPropertyName("resultcount")]
    public long Resultcount { get; set; }

    [JsonPropertyName("publishedfiledetails")]
    public WorkshopMapInfo[] Publishedfiledetails { get; set; }
}

public partial class WorkshopMapInfo
{
    [JsonPropertyName("publishedfileid")]
    public string Publishedfileid { get; set; }

    [JsonPropertyName("result")]
    public long Result { get; set; }

    [JsonPropertyName("creator")]
    public string Creator { get; set; }

    [JsonPropertyName("creator_app_id")]
    public long CreatorAppId { get; set; }

    [JsonPropertyName("consumer_app_id")]
    public long ConsumerAppId { get; set; }

    [JsonPropertyName("filename")]
    public string Filename { get; set; }

    // [JsonPropertyName("file_size")]
    // public long FileSize { get; set; }

    [JsonPropertyName("file_url")]
    public string FileUrl { get; set; }

    [JsonPropertyName("hcontent_file")]
    public string HcontentFile { get; set; }

    [JsonPropertyName("preview_url")]
    public Uri PreviewUrl { get; set; }

    [JsonPropertyName("hcontent_preview")]
    public string HcontentPreview { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; }

    [JsonPropertyName("time_created")]
    public long TimeCreated { get; set; }

    [JsonPropertyName("time_updated")]
    public long TimeUpdated { get; set; }

    [JsonPropertyName("visibility")]
    public long Visibility { get; set; }

    [JsonPropertyName("banned")]
    public long Banned { get; set; }

    [JsonPropertyName("ban_reason")]
    public string BanReason { get; set; }

    [JsonPropertyName("subscriptions")]
    public long Subscriptions { get; set; }

    [JsonPropertyName("favorited")]
    public long Favorited { get; set; }

    [JsonPropertyName("lifetime_subscriptions")]
    public long LifetimeSubscriptions { get; set; }

    [JsonPropertyName("lifetime_favorited")]
    public long LifetimeFavorited { get; set; }

    [JsonPropertyName("views")]
    public long Views { get; set; }

    [JsonPropertyName("tags")]
    public Tag[] Tags { get; set; }
}

public partial class Tag
{
    [JsonPropertyName("tag")]
    public string TagTag { get; set; }
}

 class WorkshopResponse
    {
        public WorkshopResponseResponse response { get; set; }
    }
    
    class WorkshopResponseResponse
    {
        public int result { get; set; }
        public int resultcount { get; set; }
        public WorkshopResponseCollectionDetails[] collectiondetails { get; set; }
    }
    
    class WorkshopResponseCollectionDetails
    {
        public string publishedfileid { get; set; }
        public int result { get; set; }
        public WorkshopResponseChild[] children { get; set; }
    }
    
    class WorkshopResponseChild
    {
        public string publishedfileid { get; set; }
        public int sortorder { get; set; }
        public int filetype { get; set; }
    }

   

// {
// "publishedfileid": "3130141240",
// "result": 1,
// "creator": "76561198068046327",
// "creator_app_id": 730,
// "consumer_app_id": 730,
// "filename": "",
// "file_size": 144348554,
// "file_url": "",
// "hcontent_file": "3932512874937450163",
// "preview_url": "https://steamuserimages-a.akamaihd.net/ugc/2268189945234926418/1336E1D5FFA66B520FE03AD91A228913761C3345/",
// "hcontent_preview": "2268189945234926418",
// "title": "surf_atrium",
// "description": "Newly created linear map\nDifficulty: Tier 1\nType: Linear\n\nCredits: itsTetrix, Breezy\n\nThis map has zone triggers adhering to the CS2Surf naming convention.\n\nCommands:\nsv_cheats 1;\nsv_falldamage_scale 0;\nsv_accelerate 10;\nsv_airaccelerate 850;\nsv_gravity 850.0;\nsv_enablebunnyhopping 1;\nsv_autobunnyhopping 1;\nsv_staminamax 0;\nsv_staminajumpcost 0;\nsv_staminalandcost 0;\nsv_staminarecoveryrate 0;\nmp_respawn_on_death_ct 1;\nmp_respawn_on_death_t 1;\nmp_roundtime 60;\nmp_round_restart_delay 0;\nmp_team_intro_time 0;\nmp_freezetime 0;\ncl_firstperson_legs 0;\nmp_warmup_end;\nmp_restartgame 1;",
// "time_created": 1704272581,
// "time_updated": 1704670678,
// "visibility": 0,
// "banned": 0,
// "ban_reason": "",
// "subscriptions": 3608,
// "favorited": 32,
// "lifetime_subscriptions": 3858,
// "lifetime_favorited": 34,
// "views": 1487,
// "tags": [
// {
//     "tag": "Cs2"
// },
// {
// "tag": "Map"
// },
// {
//     "tag": "Custom"
// }
// ]
// }


public class Workshop
{
    public static async Task<List<WorkshopMapInfo>> LoadMapPool(string workshopId)
    {
        using (var client = new HttpClient())
        {
            Console.WriteLine("Loading map pool");
            
            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.steampowered.com/ISteamRemoteStorage/GetCollectionDetails/v1/");
            
            // Set content
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("key", Environment.GetEnvironmentVariable("STEAM_API_KEY")!),
                new KeyValuePair<string, string>("collectioncount", "1"),
                new KeyValuePair<string, string>("publishedfileids[0]", workshopId)
            });
            request.Content = content;

            // Send request
            HttpResponseMessage response = await client.SendAsync(request);

            // Read response as json
            var responseString = await response.Content.ReadAsStringAsync();
            var responseJson = JsonSerializer.Deserialize<WorkshopResponse>(responseString);
            
            // Get map ids
            var mapIds = responseJson.response.collectiondetails[0].children.Select(x => x.publishedfileid).ToList();
            
            Console.WriteLine($"Found {mapIds.Count} maps");
            
            // Get maps
            var mapRequest = new HttpRequestMessage(HttpMethod.Post, "https://api.steampowered.com/ISteamRemoteStorage/GetPublishedFileDetails/v1/");
            
            
            var nameValueCollection = new List<KeyValuePair<string, string>>();
            nameValueCollection.Add(new KeyValuePair<string, string>("key", Environment.GetEnvironmentVariable("STEAM_API_KEY")!));
            nameValueCollection.Add(new KeyValuePair<string, string>("itemcount", mapIds.Count.ToString()));
            
            for (var i = 0; i < mapIds.Count; i++)
            {
                nameValueCollection.Add(new KeyValuePair<string, string>($"publishedfileids[{i}]", mapIds[i]));
            }
            // Set content
            var mapContent = new FormUrlEncodedContent(nameValueCollection);
            mapRequest.Content = mapContent;
            
            // Send request
            HttpResponseMessage mapResponse = await client.SendAsync(mapRequest);
            
            // Read response as json
            var mapResponseString = await mapResponse.Content.ReadAsStringAsync();
            var mapResponseJson = JsonSerializer.Deserialize<GetPublishedFileDetails>(mapResponseString);
            
            // Get maps
            var maps = mapResponseJson.Response.Publishedfiledetails.ToList();

            foreach (var map in maps)
            {
                Console.WriteLine(map.Title);
            }
            
            // Get map info
            return maps;
        }
    }
}