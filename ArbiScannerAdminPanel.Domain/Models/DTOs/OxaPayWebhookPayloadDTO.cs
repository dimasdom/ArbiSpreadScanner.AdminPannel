using System.Text.Json.Serialization;

namespace ArbiScannerAdminPanel.Domain.Models.DTOs;

public class OxaPayWebhookPayloadDTO
{
    [JsonPropertyName("track_id")]
    public string TrackId { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("order_id")]
    public string? OrderId { get; set; }

    [JsonPropertyName("date")]
    public long Date { get; set; }
}
