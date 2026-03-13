namespace PlayersManager.Models;

public class HistoricalPlayerRecord
{
    public int Id { get; set; }
    public int PlayerId { get; set; }
    public Player Player { get; set; } = null!;
    public int BatchId { get; set; }
    public Batch Batch { get; set; } = null!;
    public string Nickname { get; set; } = string.Empty;
    public string Power { get; set; } = string.Empty;
    public string TownHallLevel { get; set; } = string.Empty;
    public DateTime RecordedAt { get; set; }
}
