namespace FleetPulse.Core.Models;

public class TelemetryReading
{
    public int Id { get; set; }
    public int DeviceId { get; set; }
    public DateTimeOffset TimestampUtc { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double SpeedKph { get; set; }
    public double HeadingDegrees { get; set; }
    public double AccelerationG { get; set; }
    public double EngineRpm { get; set; }
    public bool EngineOn { get; set; }
}
