using System.Text.Json.Serialization;

namespace TeslaCam.Data;

public record class CamEvent
{
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }

    [JsonPropertyName("city")]
    public string City { get; set; }

    [JsonPropertyName("est_lat")]
    public decimal EstLat { get; set; }

    [JsonPropertyName("est_lon")]
    public decimal EstLon { get; set; }

    [JsonPropertyName("reason")]
    public string Reason { get; set; }

    [JsonPropertyName("camera")]
    public int Camera { get; set; }
}
