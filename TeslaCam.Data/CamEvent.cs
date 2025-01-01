using System.Text.Json;
using System.Text.Json.Serialization;

namespace TeslaCam.Data;

public record class CamEvent
{
    [JsonPropertyName("timestamp")]
    public required DateTime Timestamp { get; init; }

    [JsonPropertyName("city")]
    public string City { get; init; }

    [JsonPropertyName("est_lat")]
    public decimal EstLat { get; init; }

    [JsonPropertyName("est_lon")]
    public decimal EstLon { get; init; }

    [JsonPropertyName("reason")]
    public string Reason { get; init; }

    [JsonPropertyName("camera")]
    public int Camera { get; init; }

    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
    };

    public static CamEvent Deserialize(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<CamEvent>(json, JsonSerializerOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
