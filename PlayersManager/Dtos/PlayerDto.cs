using System.Text.Json.Serialization;

namespace PlayersManager.Dtos;

public class PlayerDto
{
    [JsonPropertyName("nickname")]
    public string Nickname { get; set; } = string.Empty;

    [JsonPropertyName("power")]
    public string Power { get; set; } = string.Empty;

    [JsonPropertyName("town_hall_level")]
    public string TownHallLevel { get; set; } = string.Empty;
}
