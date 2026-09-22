using FleetPulse.Core.Models;
using FleetPulse.Core.Rules;

namespace FleetPulse.Ingestion.Rules;

public class SpeedingRule : IAlertRule
{
    private const double SpeedLimitKph = 80.0;

    public IEnumerable<Alert> Evaluate(RuleEvaluationContext context)
    {
        var reading = context.Reading;

        if (reading.SpeedKph <= SpeedLimitKph)
        {
            yield break;
        }

        yield return new Alert
        {
            DeviceId = reading.DeviceId,
            Type = AlertType.Speeding,
            Severity = reading.SpeedKph > SpeedLimitKph + 20 ? AlertSeverity.Critical : AlertSeverity.Warning,
            Message = $"{DeviceNaming.NameFor(reading.DeviceId)} traveling at {reading.SpeedKph:F0} km/h (limit {SpeedLimitKph:F0})",
            Latitude = reading.Latitude,
            Longitude = reading.Longitude,
            TimestampUtc = reading.TimestampUtc
        };
    }
}
