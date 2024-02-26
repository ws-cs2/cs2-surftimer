using System.Text.Json;

namespace WST;

public class V_RankedRecord
{
    public int Id { get; set; }
    public string SteamId { get; set; }
    public string MapName { get; set; }
    public string Route { get; set; }
    public int Ticks { get; set; }
    public string Name { get; set; }
    public int Position { get; set; }
    public int TotalRecords { get; set; }
    
    public string Style { get; set; }
    
    public float VelocityStartXY { get; set; }
    public float VelocityStartZ { get; set; }
    public float VelocityStartXYZ { get; set; }
    public float VelocityEndXY { get; set; }
    public float VelocityEndZ { get; set; }
    public float VelocityEndXYZ { get; set; }
    
    public string Checkpoints { get; set; }
    
    public int ScaleFactor { get; set; }
    
    public int Tier { get; set; }
    
    public int BasicPoints { get; set; }
    public int BonusPoints { get; set; }

    public int Points
    {
        get => BasicPoints + BonusPoints;
    }



    public List<Checkpoint> CheckpointsObject
    {
        get => JsonSerializer.Deserialize<List<Checkpoint>>(Checkpoints);
    }

    public string FormatForChat()
    {
        var playerName = this.Name;
        if (playerName.Length > 20)
        {
            playerName = playerName.Substring(0, 20);
        }
        var time = Utils.FormatTime(this.Ticks);
        var rank = this.Position;
        var points = this.Points;
                    
        var rankString =
            $"{CC.Secondary}{rank}{CC.White}/{CC.Secondary}{this.TotalRecords}{CC.White}";

        return
            $" {CC.Main}{time}{CC.White} {CC.Secondary}{playerName}{CC.White} (rank {rankString}) {CC.Secondary}{points}{CC.White} points";
    }
}