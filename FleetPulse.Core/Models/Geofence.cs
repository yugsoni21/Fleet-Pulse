namespace FleetPulse.Core.Models;

public class Geofence
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public double CenterLatitude { get; set; }
    public double CenterLongitude { get; set; }
    public double RadiusMeters { get; set; }
}
