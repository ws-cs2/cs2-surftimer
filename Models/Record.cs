using System.Text.Json;

namespace WST;

public class Record
{
    public int Id { get; set; } // Maps to the 'id' column
    public string SteamId { get; set; } // Maps to the 'steam_id' column
    public string MapName { get; set; } // Maps to the 'mapname' column
    public string Route { get; set; }
    public int Ticks { get; set; } // Maps to the 'time' column
    public string Name { get; set; } // Maps to the 'name' column

    public float VelocityStartXY { get; set; }
    public float VelocityStartZ { get; set; }
    public float VelocityStartXYZ { get; set; }
    public float VelocityEndXY { get; set; }
    public float VelocityEndZ { get; set; }
    public float VelocityEndXYZ { get; set; }

    public string Checkpoints { get; set; }

    public List<Checkpoint> CheckpointsObject
    {
        get => JsonSerializer.Deserialize<List<Checkpoint>>(Checkpoints);
    }
}