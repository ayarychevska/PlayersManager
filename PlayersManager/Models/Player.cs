namespace PlayersManager.Models;

public class Player
{
    public int Id { get; set; }
    public string Nickname { get; set; } = string.Empty;
    public PlayerStatus Status { get; set; } = PlayerStatus.Active;
    public List<HistoricalPlayerRecord> History { get; set; } = [];
}
