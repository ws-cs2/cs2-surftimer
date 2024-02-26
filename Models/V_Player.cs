namespace WST;

public class V_Player
{
    public Int64  Id  { get; set; }
    public string SteamId { get; set; }
    public string Name { get; set; }
    public bool IsAdmin { get; set; }
    public int PPoints { get; set; }
    public int LgPoints { get; set; }
    public int TmPoints { get; set; }
    public int SwPoints { get; set; }
    public int HswPoints { get; set; }
    public int PracPoints { get; set; }
    public int Rank { get; set; }
    
    public int LgRank { get; set; }
    
    public int TmRank { get; set; }
    public int SwRank { get; set; }
    public int HswRank { get; set; }
    public int PracRank { get; set; }
    public int TotalPlayers { get; set; }
    
    public string? CustomHudUrl { get; set; }

    public bool IsVip { get; set; }
    
    public string? CustomTag { get; set; }
    
    public string? ChatColor { get; set; }
    
    public string? NameColor { get; set; }
}