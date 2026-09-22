using FleetPulse.Core.Models;
using FleetPulse.Core.Rules;
using FleetPulse.Ingestion.Rules;
using Xunit;

namespace FleetPulse.Tests.Rules;

public class GeofenceRuleTests
{
    private static readonly Geofence Depot = new()
    {
        Id = 1,
        Name = "Depot",
        CenterLatitude = 43.6532,
        CenterLongitude = -79.3832,
        RadiusMeters = 500
    };

    private static RuleEvaluationContext ContextFor(double lat, double lng, DateTimeOffset? timestamp = null) => new()
    {
        Reading = new TelemetryReading
        {
            DeviceId = 1,
            Latitude = lat,
            Longitude = lng,
            TimestampUtc = timestamp ?? DateTimeOffset.UtcNow
        },
        Geofences = new[] { Depot }
    };

    [Fact]
    public void IsInside_ReturnsTrue_ForPointAtCenter()
    {
        Assert.True(GeofenceRule.IsInside(Depot.CenterLatitude, Depot.CenterLongitude, Depot));
    }

    [Fact]
    public void IsInside_ReturnsFalse_ForPointFarAway()
    {
        Assert.False(GeofenceRule.IsInside(43.7, -79.5, Depot));
    }

    [Fact]
    public void Evaluate_FiresEntryAlert_OnTransitionIntoZone()
    {
        var rule = new GeofenceRule();

        var outside = rule.Evaluate(ContextFor(43.7, -79.5)).ToList();
        Assert.Empty(outside);

        var inside = rule.Evaluate(ContextFor(Depot.CenterLatitude, Depot.CenterLongitude)).ToList();

        var alert = Assert.Single(inside);
        Assert.Equal(AlertType.GeofenceEntry, alert.Type);
    }

    [Fact]
    public void Evaluate_FiresExitAlert_OnTransitionOutOfZone()
    {
        var rule = new GeofenceRule();

        rule.Evaluate(ContextFor(Depot.CenterLatitude, Depot.CenterLongitude)).ToList();
        var exit = rule.Evaluate(ContextFor(43.7, -79.5)).ToList();

        var alert = Assert.Single(exit);
        Assert.Equal(AlertType.GeofenceExit, alert.Type);
    }

    [Fact]
    public void Evaluate_DoesNotRefire_WhileStayingInsideZone()
    {
        var rule = new GeofenceRule();

        rule.Evaluate(ContextFor(Depot.CenterLatitude, Depot.CenterLongitude)).ToList();
        var second = rule.Evaluate(ContextFor(Depot.CenterLatitude, Depot.CenterLongitude)).ToList();

        Assert.Empty(second);
    }
}
