using System.Text.Json.Serialization;

namespace PlayersManager.Dtos;

public class UpdateNicknameDto
{
    [JsonPropertyName("nickname")]
    public string Nickname { get; set; } = string.Empty;
}
