using FleetPulse.Core.Models;
using FleetPulse.Core.Rules;

namespace FleetPulse.Ingestion.Rules;

public class HarshEventRule : IAlertRule
{
    private const double HarshBrakeThresholdG = -0.5;
    private const double HarshAccelThresholdG = 0.5;

    public IEnumerable<Alert> Evaluate(RuleEvaluationContext context)
    {
        var reading = context.Reading;

        if (reading.AccelerationG <= HarshBrakeThresholdG)
        {
            yield return new Alert
            {
                DeviceId = reading.DeviceId,
                Type = AlertType.HarshBraking,
                Severity = AlertSeverity.Warning,
                Message = $"{DeviceNaming.NameFor(reading.DeviceId)} harsh braking event ({reading.AccelerationG:F2}g)",
                Latitude = reading.Latitude,
                Longitude = reading.Longitude,
                TimestampUtc = reading.TimestampUtc
            };
        }
        else if (reading.AccelerationG >= HarshAccelThresholdG)
        {
            yield return new Alert
            {
                DeviceId = reading.DeviceId,
                Type = AlertType.HarshAcceleration,
                Severity = AlertSeverity.Warning,
                Message = $"{DeviceNaming.NameFor(reading.DeviceId)} harsh acceleration event ({reading.AccelerationG:F2}g)",
                Latitude = reading.Latitude,
                Longitude = reading.Longitude,
                TimestampUtc = reading.TimestampUtc
            };
        }
    }
}
