using System.IO.Compression;
using CounterStrikeSharp.API;

namespace WST;

public class V_ReplayWithPosition : V_Replay
{
    public int ReplayPositionInRecords { get; set; }
}

public class V_Replay
{
    public int Id { get; set; }
    public string SteamId { get; set; }
    public string MapName { get; set; }
    public string Route { get; set; }
    public int Ticks { get; set; }
    public byte[]? Replay { get; set; }
    public string PlayerName { get; set; }
    
    public string? CustomHudUrl { get; set; }
    
    public string? ReplayUrl { get; set; }
    
    public int Position { get; set; } // position in ranked_records
    public int TotalRecords { get; set; } 
    
    public string Style { get; set; } = "";

    public Task Delete(Database database)
    {
        return database.ExecuteAsync("DELETE FROM replays WHERE id = @id", new { id = Id });
    }

    public static async Task<V_Replay> GetReplay(Database database, string mapName, string route, string steamId, string style)
    {
        try
        {
            var result = await database.QueryAsync<V_Replay>(
                "SELECT * FROM public.v_replays WHERE mapname = @mapname AND route = @route AND steam_id = @steamid and style = @style",
                new { mapname = mapName, route = route, steamid = steamId, style });

            var res = result.FirstOrDefault();

            if (res != null && res.ReplayUrl != null)
            {
                await JuiceReplayFromS3(res);
            }

            return res;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return null;
        }
}
    
    public static async Task<V_Replay> GetRecordReplay(Database database, string mapName, string route, int position, string style)
    {
        try
        {
            var result = await database.QueryAsync<V_Replay>(
                @"WITH DesiredRank AS (
SELECT
    ticks, mapname, route, style
FROM
    public.v_ranked_records
WHERE
    position = @position
    AND mapname = @mapname
    AND route = @route
    and style = @style
),
ClosestReplay AS (
SELECT
    r.*,
    ABS(r.ticks - DR.ticks) AS ticks_difference
FROM
    public.v_replays r
CROSS JOIN DesiredRank DR
WHERE
    r.mapname = DR.mapname
    AND r.route = DR.route
    and r.style = DR.style
ORDER BY
    ticks_difference ASC
LIMIT 1
)
SELECT
    CR.*
FROM
    ClosestReplay CR;",
                new { mapname = mapName, route = route, position = position, style = style });

            var res = result.FirstOrDefault();

            if (res != null && res.ReplayUrl != null)
            {
                await JuiceReplayFromS3(res);
            }

            return res;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return null;
        }
    }

    private static async Task JuiceReplayFromS3(V_Replay res)
    {
        var supabase = Wst.Instance._supabaseClient;
        var replayBytes = await supabase.Storage.From("replays").Download(res.ReplayUrl.Replace("replays/", ""), null);
        if (replayBytes == null)
        {
           throw new Exception("Replay not found");
        }

        using var compressedStream = new MemoryStream(replayBytes);
        using var decompressedStream = new MemoryStream();
        using (var gzipStream = new GZipStream(compressedStream, CompressionMode.Decompress))
        {
            gzipStream.CopyTo(decompressedStream);
        }

        var decompressedBytes = decompressedStream.ToArray();

        res.Replay = decompressedBytes;
    }
}
