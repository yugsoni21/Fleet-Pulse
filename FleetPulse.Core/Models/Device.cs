namespace FleetPulse.Core.Models;

public class Device
{
    public int Id { get; set; }
    public string SerialNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string VehicleType { get; set; } = "Van";
    public double CurrentLatitude { get; set; }
    public double CurrentLongitude { get; set; }
    public double CurrentSpeedKph { get; set; }
    public double CurrentHeadingDegrees { get; set; }
    public bool IsIdle { get; set; }
    public DateTimeOffset LastSeenUtc { get; set; }
}
