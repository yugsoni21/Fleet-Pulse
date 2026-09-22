using System.Collections.Concurrent;
using FleetPulse.Core;
using FleetPulse.Core.Models;
using FleetPulse.Core.Rules;

namespace FleetPulse.Ingestion.Rules;

/// <summary>
/// Stateful rule: remembers each device's last known inside/outside status per evaluation
/// run so it only fires on the transition (entry/exit), not on every reading inside a zone.
/// </summary>
public class GeofenceRule : IAlertRule
{
    private readonly ConcurrentDictionary<int, bool> _insideState = new();

    public IEnumerable<Alert> Evaluate(RuleEvaluationContext context)
    {
        if (context.Geofences.Count == 0)
        {
            yield break;
        }

        var reading = context.Reading;
        var isInsideNow = context.Geofences.Any(g => IsInside(reading.Latitude, reading.Longitude, g));
        var wasInside = _insideState.GetOrAdd(reading.DeviceId, isInsideNow);

        if (isInsideNow == wasInside)
        {
            yield break;
        }

        _insideState[reading.DeviceId] = isInsideNow;

        yield return new Alert
        {
            DeviceId = reading.DeviceId,
            Type = isInsideNow ? AlertType.GeofenceEntry : AlertType.GeofenceExit,
            Severity = AlertSeverity.Info,
            Message = $"{DeviceNaming.NameFor(reading.DeviceId)} {(isInsideNow ? "entered" : "exited")} a geofenced zone",
            Latitude = reading.Latitude,
            Longitude = reading.Longitude,
            TimestampUtc = reading.TimestampUtc
        };
    }

    public static bool IsInside(double lat, double lng, Geofence geofence)
    {
        var distance = GeoMath.DistanceMeters(lat, lng, geofence.CenterLatitude, geofence.CenterLongitude);
        return distance <= geofence.RadiusMeters;
    }
}
