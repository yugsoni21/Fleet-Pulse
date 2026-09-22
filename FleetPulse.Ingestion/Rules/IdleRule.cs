using System.Collections.Concurrent;
using FleetPulse.Core.Models;
using FleetPulse.Core.Rules;

namespace FleetPulse.Ingestion.Rules;

/// <summary>
/// Stateful rule: tracks how long each device has been below the idle speed threshold and
/// fires exactly once per idle episode when the duration crosses the threshold.
/// </summary>
public class IdleRule : IAlertRule
{
    private const double IdleSpeedThresholdKph = 1.0;
    private static readonly TimeSpan IdleThreshold = TimeSpan.FromSeconds(20);

    private readonly ConcurrentDictionary<int, DateTimeOffset> _idleSince = new();
    private readonly ConcurrentDictionary<int, bool> _alerted = new();

    public IEnumerable<Alert> Evaluate(RuleEvaluationContext context)
    {
        var reading = context.Reading;

        if (reading.SpeedKph > IdleSpeedThresholdKph)
        {
            _idleSince.TryRemove(reading.DeviceId, out _);
            _alerted.TryRemove(reading.DeviceId, out _);
            yield break;
        }

        var since = _idleSince.GetOrAdd(reading.DeviceId, reading.TimestampUtc);
        var idleDuration = reading.TimestampUtc - since;

        if (idleDuration < IdleThreshold || !_alerted.TryAdd(reading.DeviceId, true))
        {
            yield break;
        }

        yield return new Alert
        {
            DeviceId = reading.DeviceId,
            Type = AlertType.ExcessiveIdle,
            Severity = AlertSeverity.Info,
            Message = $"{DeviceNaming.NameFor(reading.DeviceId)} idling for over {idleDuration.TotalSeconds:F0}s",
            Latitude = reading.Latitude,
            Longitude = reading.Longitude,
            TimestampUtc = reading.TimestampUtc
        };
    }
}
