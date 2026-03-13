using System.Text.Json.Serialization;

namespace PlayersManager.Dtos;

public class ImportBatchDto
{
    [JsonPropertyName("date")]
    public DateTime Date { get; set; }

    [JsonPropertyName("players")]
    public List<PlayerDto> Players { get; set; } = [];
}
