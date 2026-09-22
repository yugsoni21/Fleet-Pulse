using FleetPulse.Core;
using Xunit;

namespace FleetPulse.Tests;

public class GeoMathTests
{
    [Fact]
    public void DistanceMeters_IsZero_ForIdenticalPoints()
    {
        Assert.Equal(0, GeoMath.DistanceMeters(43.65, -79.38, 43.65, -79.38), 3);
    }

    [Fact]
    public void DistanceMeters_MatchesKnownSeparation()
    {
        // One degree of latitude is ~111 km anywhere on the globe.
        var distance = GeoMath.DistanceMeters(43.0, -79.38, 44.0, -79.38);
        Assert.InRange(distance, 110_000, 112_000);
    }

    [Theory]
    [InlineData(44.0, -79.38, 0)]    // due north
    [InlineData(43.0, -79.38, 180)]  // due south
    public void BearingDegrees_ReturnsCardinalDirections(double toLat, double toLng, double expected)
    {
        var bearing = GeoMath.BearingDegrees(43.5, -79.38, toLat, toLng);
        Assert.Equal(expected, bearing, 1);
    }

    [Fact]
    public void BearingDegrees_ReturnsRoughlyEast_ForEastwardTravel()
    {
        var bearing = GeoMath.BearingDegrees(43.65, -79.5, 43.65, -79.3);
        Assert.InRange(bearing, 88, 92);
    }
}
