using System.Text.Json;
using System.Text.Json.Serialization;
using CounterStrikeSharp.API.Modules.Utils;

namespace WST;

public class MapZone
{
    public List<Route> Routes { get; set; }
    
    [JsonIgnore]
    public Route Main => Routes.First(x => x.Key == "main");
    
    public Route? GetRoute(string key)
    {
        return Routes.FirstOrDefault(x => x.Key == key);
    }
    
    public List<Route> GetStageRoutes()
    {
        return Routes.Where(x => x.Key.StartsWith("s")).ToList();
    }
    
    public List<string> GetRouteKeys()
    {
        var keys = Routes.Select(x => x.Key).ToList();
        return keys;
    }
    
    [JsonIgnore]
    public Dictionary<string, Dictionary<string, V_RankedRecord>> WorldRecords { get; set; } = new();

    public async Task LoadRecords(Database db, string currentMap)
    {
        var records = (await db.QueryAsync<V_RankedRecord>("SELECT * FROM v_ranked_records WHERE mapname = @mapname and position = 1",
            new { mapname = currentMap })).ToList();
        foreach (var record in records)
        {
            WorldRecords[record.Route] = WorldRecords.GetValueOrDefault(record.Route, new Dictionary<string, V_RankedRecord>());
            WorldRecords[record.Route][record.Style] = record;
        }
    }
    
   

    public async Task Save(Database database, string mapName)
    {
        try
        {
            foreach (var routeData in this.Routes)
            {
                var routeJson = JsonSerializer.Serialize(routeData,
                    new JsonSerializerOptions
                        { WriteIndented = true, DictionaryKeyPolicy = JsonNamingPolicy.CamelCase });

                await database.ExecuteAsync(@"
            INSERT INTO maps_2 (name, route, route_data) 
            VALUES (@name, @route, CAST(@route_data as jsonb))
            ON CONFLICT (name, route) 
            DO UPDATE SET 
                route_data = EXCLUDED.route_data",
                    new { name = mapName, route = routeData.Key, route_data = routeJson });
                
                Console.WriteLine($"Saved route {routeData.Key}");
                
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
       
    }
    
    public V_RankedRecord? GetWorldRecord(string route, string style)
    {
        if (WorldRecords.ContainsKey(route))
        {
            if (WorldRecords[route].ContainsKey(style))
            {
                return WorldRecords[route][style];
            }
        }
        
        return null;
    }
    
}

public enum RouteType
{
    stage,
    linear
}

public class Route
{
    public string Key { get; set; }


    public string Name { get; set; }


    [JsonConverter(typeof(JsonStringEnumConverter))]
    public RouteType Type { get; set; }
    
    public ZoneVector StartPos { get; set; }
    
    public ZoneVector StartVelocity { get; set; }
    public ZoneVector StartAngles { get; set; }

    public float VelocityCapStartXY { get; set; } = 350;
    public Zone Start { get; set; }

    public Zone End { get; set; }

    public int Tier { get; set; } = 1;
    public List<Zone> Checkpoints { get; set; }
    
}

public enum ZoneType
{
    trigger,
    vector
}

public class ZoneVector
{
    public ZoneVector()
    {
        
    }
    public ZoneVector(float x, float y, float z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }

    public float x { get; set; }
    public float y { get; set; }
    public float z { get; set; }
    
    public Vector ToNativeVector()
    {
        return new Vector(x, y, z);
    }
    
    public QAngle ToNativeQAngle()
    {
        return new QAngle(x, y, z);
    }
    
    public string ToVectorString()
    {
        return $"{x} {y} {z}";
    }
}

public class Zone
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ZoneType Type { get; set; }

    public string? TargetName { get; set; }

    public ZoneVector v1 { get; set; }

    public ZoneVector v2 { get; set; }

    [JsonIgnore]
    public Wst.TriggerInfo TriggerInfo { get; set; }
    
}