using FleetPulse.Core.Models;
using FleetPulse.Core.Rules;
using FleetPulse.Ingestion.Rules;
using Xunit;

namespace FleetPulse.Tests.Rules;

public class IdleRuleTests
{
    private static RuleEvaluationContext ContextFor(double speedKph, DateTimeOffset timestamp) => new()
    {
        Reading = new TelemetryReading
        {
            DeviceId = 1,
            SpeedKph = speedKph,
            TimestampUtc = timestamp
        }
    };

    [Fact]
    public void Evaluate_DoesNotFire_BeforeThresholdElapsed()
    {
        var rule = new IdleRule();
        var start = DateTimeOffset.UtcNow;

        var first = rule.Evaluate(ContextFor(0, start)).ToList();
        var second = rule.Evaluate(ContextFor(0, start.AddSeconds(5))).ToList();

        Assert.Empty(first);
        Assert.Empty(second);
    }

    [Fact]
    public void Evaluate_Fires_AfterThresholdElapsed()
    {
        var rule = new IdleRule();
        var start = DateTimeOffset.UtcNow;

        rule.Evaluate(ContextFor(0, start)).ToList();
        var afterThreshold = rule.Evaluate(ContextFor(0, start.AddSeconds(25))).ToList();

        var alert = Assert.Single(afterThreshold);
        Assert.Equal(AlertType.ExcessiveIdle, alert.Type);
    }

    [Fact]
    public void Evaluate_DoesNotRefire_ForSameIdleEpisode()
    {
        var rule = new IdleRule();
        var start = DateTimeOffset.UtcNow;

        rule.Evaluate(ContextFor(0, start)).ToList();
        rule.Evaluate(ContextFor(0, start.AddSeconds(25))).ToList();
        var third = rule.Evaluate(ContextFor(0, start.AddSeconds(30))).ToList();

        Assert.Empty(third);
    }

    [Fact]
    public void Evaluate_ResetsIdleTimer_WhenDeviceMoves()
    {
        var rule = new IdleRule();
        var start = DateTimeOffset.UtcNow;

        rule.Evaluate(ContextFor(0, start)).ToList();
        rule.Evaluate(ContextFor(50, start.AddSeconds(10))).ToList();
        var afterMoving = rule.Evaluate(ContextFor(0, start.AddSeconds(15))).ToList();

        Assert.Empty(afterMoving);
    }
}
