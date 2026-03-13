namespace PlayersManager.Models;

public class BatchRecord
{
    public int Id { get; set; }
    public int BatchId { get; set; }
    public Batch Batch { get; set; } = null!;
    public string Nickname { get; set; } = string.Empty;
    public string Power { get; set; } = string.Empty;
    public string TownHallLevel { get; set; } = string.Empty;
    public MatchStatus MatchStatus { get; set; } = MatchStatus.NotMatched;
}
