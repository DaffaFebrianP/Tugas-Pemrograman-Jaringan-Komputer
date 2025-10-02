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
    //Menerima jika bukan PM
    public string? To { get; set; }

    // Isi pesan
    [JsonPropertyName("text")]
    //Menerima jika pesan hanya userlist/typing
    public string? Text { get; set; }

    [JsonPropertyName("ts")]
    public long Ts { get; set; }

    // userlist
    [JsonPropertyName("users")]
    //Menerima jika bukan pesan userlist
    public List<string>? Users { get; set; }

    // indikator ngetik
    [JsonPropertyName("isTyping")]
    //Hanya terisi untuk Type="typing"
    public bool? IsTyping { get; set; }

    public Message()
    {
        // Tetapkan timestamp saat objek dibuat
        Ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }
}