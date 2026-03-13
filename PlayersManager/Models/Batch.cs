namespace PlayersManager.Models;

public class Batch
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public List<BatchRecord> Records { get; set; } = [];
}
