namespace PlayersManager.Dtos;

public class HistoricalRecordDto
{
    public int PlayerId { get; set; }
    public int BatchId { get; set; }
    public string Nickname { get; set; } = string.Empty;
    public string Power { get; set; } = string.Empty;
    public string TownHallLevel { get; set; } = string.Empty;
    public DateTime RecordedAt { get; set; }
}
