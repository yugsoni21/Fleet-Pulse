namespace FleetPulse.Core.Models;

public enum AlertType
{
    Speeding,
    HarshBraking,
    HarshAcceleration,
    GeofenceEntry,
    GeofenceExit,
    ExcessiveIdle
}

public enum AlertSeverity
{
    Info,
    Warning,
    Critical
}

public class Alert
{
    public int Id { get; set; }
    public int DeviceId { get; set; }
    public AlertType Type { get; set; }
    public AlertSeverity Severity { get; set; }
    public string Message { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public DateTimeOffset TimestampUtc { get; set; }
}
