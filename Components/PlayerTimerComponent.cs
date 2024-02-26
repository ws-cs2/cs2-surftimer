using CounterStrikeSharp.API.Modules.Utils;

namespace WST;

public class CheckpointTimerComponent
{
    public int TimerTicks { get; set; }
    public float VelocityStartXY { get; set; }
    public float VelocityStartZ { get; set; }
    public float VelocityStartXYZ { get; set; }
    
    public CheckpointTimerComponent DeepCopy()
    {
        return new CheckpointTimerComponent
        {
            TimerTicks = this.TimerTicks,
            VelocityStartXY = this.VelocityStartXY,
            VelocityStartZ = this.VelocityStartZ,
            VelocityStartXYZ = this.VelocityStartXYZ
        };
    }

}
public class TimerData
{
    public int TimerTicks { get; set; } = 0;
    public float VelocityStartXY { get; set; }
    public float VelocityStartZ { get; set; }
    public float VelocityStartXYZ { get; set; }
    public float VelocityEndXY { get; set; }
    public float VelocityEndZ { get; set; }
    public float VelocityEndXYZ { get; set; }
    public List<CheckpointTimerComponent> Checkpoints { get; set; } = new();
    public Replay Recording { get; set; } = new();
    public string RouteKey { get; set; }
    
    public TimerData DeepCopy()
    {
        var copy = new TimerData
        {
            TimerTicks = this.TimerTicks,
            VelocityStartXY = this.VelocityStartXY,
            VelocityStartZ = this.VelocityStartZ,
            VelocityStartXYZ = this.VelocityStartXYZ,
            VelocityEndXY = this.VelocityEndXY,
            VelocityEndZ = this.VelocityEndZ,
            VelocityEndXYZ = this.VelocityEndXYZ,
            Checkpoints = this.Checkpoints.Select(cp => cp.DeepCopy()).ToList(),
            RouteKey = this.RouteKey,
            Recording = this.Recording.DeepCopy()
        };

        return copy;
    }
}

public class PlayerTimerComponent
{
    public TimerData Primary { get; set; } = new();
    
    public TimerData? Secondary { get; set; } = null;

    // turbo master shit
    public bool TmInSlowMo { get; set; }
    public float TmXYSpeedBeforeSlowMo { get; set; }
    public float TmZSpeedBeforeSlowMo { get; set; }
    
    public PlayerTimerComponent DeepCopy()
    {
        return new PlayerTimerComponent
        {
            Primary = this.Primary.DeepCopy(),
            Secondary = this.Secondary?.DeepCopy(),
            TmInSlowMo = this.TmInSlowMo,
            TmXYSpeedBeforeSlowMo = this.TmXYSpeedBeforeSlowMo,
            TmZSpeedBeforeSlowMo = this.TmZSpeedBeforeSlowMo
        };
    }
    
}