namespace TeslaCam.Data;

public record class CamEvent
{
    public DateTime Timestamp { get; set; }
    public string City { get; set; }
    public double EstLat { get; set; }
    public double EstLon { get; set; }
    public string Reason { get; set; }
    public int Camera { get; set; }
}
