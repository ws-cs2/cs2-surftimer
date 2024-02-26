namespace WST;

public class GameServer
{
    public string ServerId { get; set; }
    public string WorkshopCollection { get; set; }
    public string Hostname { get; set; }
    public bool IsPublic { get; set; }
    public string IP { get; set; }
    
    public string? CurrentMap { get; set; }
    
    public int PlayerCount { get; set; }
    
    public int TotalPlayers { get; set; }
    
    public string ShortName { get; set; }
    
    public bool Vip { get; set; }
    
    public string Style { get; set; }
}