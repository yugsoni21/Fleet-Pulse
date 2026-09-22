using FleetPulse.Core.Models;

namespace FleetPulse.Core.Rules;

public class RuleEvaluationContext
{
    public required TelemetryReading Reading { get; init; }
    public IReadOnlyList<Geofence> Geofences { get; init; } = Array.Empty<Geofence>();
}
