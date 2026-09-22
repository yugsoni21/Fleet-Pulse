using FleetPulse.Core.Models;
using FleetPulse.Core.Rules;
using FleetPulse.Ingestion.Rules;
using Xunit;

namespace FleetPulse.Tests.Rules;

public class SpeedingRuleTests
{
    private static RuleEvaluationContext ContextFor(double speedKph) => new()
    {
        Reading = new TelemetryReading
        {
            DeviceId = 1,
            SpeedKph = speedKph,
            TimestampUtc = DateTimeOffset.UtcNow
        }
    };

    [Theory]
    [InlineData(40)]
    [InlineData(80)]
    public void Evaluate_DoesNotFire_WhenAtOrBelowLimit(double speed)
    {
        var rule = new SpeedingRule();
        var alerts = rule.Evaluate(ContextFor(speed));
        Assert.Empty(alerts);
    }

    [Theory]
    [InlineData(85, AlertSeverity.Warning)]
    [InlineData(105, AlertSeverity.Critical)]
    public void Evaluate_Fires_WhenOverLimit(double speed, AlertSeverity expectedSeverity)
    {
        var rule = new SpeedingRule();
        var alerts = rule.Evaluate(ContextFor(speed)).ToList();

        var alert = Assert.Single(alerts);
        Assert.Equal(AlertType.Speeding, alert.Type);
        Assert.Equal(expectedSeverity, alert.Severity);
    }
}
