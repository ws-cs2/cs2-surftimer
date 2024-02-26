namespace WST;

public class RecentRecord 
{
    public int Id { get; set; }
    public string SteamId { get; set; }
    public string MapName { get; set; }
    public string Route { get; set; }
    public int Ticks { get; set; }
    public string Name { get; set; }
    public string NewName { get; set; }
    public int NewTicks { get; set; }
    public string OldName { get; set; }
    public int? OldTicks { get; set; } // Nullable as the column is nullable
    public DateTime CreatedAt { get; set; }
    public string OldSteamId { get; set; } // Nullable as the column is nullable
}