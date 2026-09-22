using FleetPulse.Core.Models;
using FleetPulse.Core.Rules;
using FleetPulse.Ingestion.Rules;
using Xunit;

namespace FleetPulse.Tests.Rules;

public class HarshEventRuleTests
{
    private static RuleEvaluationContext ContextFor(double accelerationG) => new()
    {
        Reading = new TelemetryReading
        {
            DeviceId = 1,
            AccelerationG = accelerationG,
            TimestampUtc = DateTimeOffset.UtcNow
        }
    };

    [Theory]
    [InlineData(0.0)]
    [InlineData(0.3)]
    [InlineData(-0.3)]
    public void Evaluate_DoesNotFire_ForNormalDriving(double accelerationG)
    {
        var rule = new HarshEventRule();
        Assert.Empty(rule.Evaluate(ContextFor(accelerationG)));
    }

    [Fact]
    public void Evaluate_FiresHarshBraking_OnStrongDeceleration()
    {
        var rule = new HarshEventRule();

        var alert = Assert.Single(rule.Evaluate(ContextFor(-0.8)).ToList());

        Assert.Equal(AlertType.HarshBraking, alert.Type);
        Assert.Equal(AlertSeverity.Warning, alert.Severity);
    }

    [Fact]
    public void Evaluate_FiresHarshAcceleration_OnStrongAcceleration()
    {
        var rule = new HarshEventRule();

        var alert = Assert.Single(rule.Evaluate(ContextFor(0.8)).ToList());

        Assert.Equal(AlertType.HarshAcceleration, alert.Type);
    }
}
