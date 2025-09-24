using System.Text.Json.Serialization;

public class Message
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "";

    [JsonPropertyName("from")]
    public string From { get; set; } = "";

    [JsonPropertyName("to")]
    public string? To { get; set; }

    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("ts")]
    public long Ts { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    
    [JsonPropertyName("users")]
    public List<string>? Users { get; set; }
}
