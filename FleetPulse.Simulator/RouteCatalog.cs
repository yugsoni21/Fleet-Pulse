namespace FleetPulse.Simulator;

public record Waypoint(double Latitude, double Longitude);

public record RouteDefinition(int Id, string Name, IReadOnlyList<Waypoint> Waypoints);

/// <summary>
/// A handful of hardcoded closed-loop routes (Toronto-area coordinates) that virtual
/// devices drive around continuously.
/// </summary>
public static class RouteCatalog
{
    public static readonly IReadOnlyList<RouteDefinition> Routes = new List<RouteDefinition>
    {
        new(1, "Downtown Loop", new List<Waypoint>
        {
            new(43.6532, -79.3832),
            new(43.6560, -79.3803),
            new(43.6591, -79.3776),
            new(43.6575, -79.3730),
            new(43.6540, -79.3750),
            new(43.6510, -79.3790),
            new(43.6532, -79.3832)
        }),
        new(2, "Airport Run", new List<Waypoint>
        {
            new(43.6777, -79.6248),
            new(43.6700, -79.5900),
            new(43.6600, -79.5500),
            new(43.6500, -79.5100),
            new(43.6450, -79.4700),
            new(43.6500, -79.4300),
            new(43.6532, -79.3832),
            new(43.6600, -79.5000),
            new(43.6777, -79.6248)
        }),
        new(3, "Industrial Corridor", new List<Waypoint>
        {
            new(43.7000, -79.4000),
            new(43.7200, -79.3700),
            new(43.7400, -79.3400),
            new(43.7300, -79.3100),
            new(43.7100, -79.3300),
            new(43.7000, -79.4000)
        })
    };
}
