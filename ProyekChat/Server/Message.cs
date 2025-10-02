using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

public class Message
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "";

    // Pengirim pesan
    [JsonPropertyName("from")]
    public string From { get; set; } = "";

    // Penerima pesan pribadi
    [JsonPropertyName("to")]
    public string? To { get; set; }

    // Isi pesan
    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("ts")]
    public long Ts { get; set; }

    // userlist
    [JsonPropertyName("users")]
    public List<string>? Users { get; set; }

    // indikator ngetik
    [JsonPropertyName("isTyping")]
    public bool? IsTyping { get; set; }

    public Message()
    {
        Ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }
}
