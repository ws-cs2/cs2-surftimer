namespace WST;

public class PlayerReplayComponent
{
    public PlayerReplayComponent(Replay replay, int tick, string route, string playerName, string style, string? customHudUrl)
    {
        Replay = replay;
        ReplayPlayback = new ReplayPlayback(replay);
        Tick = tick;
        Route = route;
        PlayerName = playerName;
        TotalTicks = replay.Frames.Count;
        CustomHudUrl = customHudUrl;
        Style = style;
    }
    
    // replay from DB
    public Replay? Replay { get;  }

    // replay playback
    public ReplayPlayback ReplayPlayback { get;  }
    
    public int Tick { get; set; } = 0;

    public string Route { get; set; } = "";
    
    public string Style { get; set; } = "";
    public string PlayerName { get; set; } = "";
    
    public int TotalTicks { get; set; } = 0;
    
    public string? CustomHudUrl { get; set; }
}