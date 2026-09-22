using FleetPulse.Core;
using FleetPulse.Simulator;
using Xunit;

namespace FleetPulse.Tests;

public class SimulatorTests
{
    [Fact]
    public void Advance_MovesAtPlausibleRoadSpeed()
    {
        var device = new VirtualDevice(1, RouteCatalog.Routes[0], 0);
        var random = new Random(1234);

        var previous = device.Advance(random, 1.0);

        for (var i = 0; i < 500; i++)
        {
            var current = device.Advance(random, 1.0);

            var metres = GeoMath.DistanceMeters(
                previous.Latitude, previous.Longitude,
                current.Latitude, current.Longitude);

            // A vehicle at even 150 km/h covers ~42 m per second. Anything beyond that means
            // route progress is no longer scaled by real leg distance.
            Assert.True(metres < 60, $"Device jumped {metres:F0}m in one second tick");

            previous = current;
        }
    }

    [Fact]
    public void Advance_StaysWithinItsRouteBoundingBox()
    {
        var route = RouteCatalog.Routes[2];
        var device = new VirtualDevice(7, route, 0.3);
        var random = new Random(99);

        var minLat = route.Waypoints.Min(w => w.Latitude) - 0.001;
        var maxLat = route.Waypoints.Max(w => w.Latitude) + 0.001;
        var minLng = route.Waypoints.Min(w => w.Longitude) - 0.001;
        var maxLng = route.Waypoints.Max(w => w.Longitude) + 0.001;

        for (var i = 0; i < 1000; i++)
        {
            var reading = device.Advance(random, 1.0);

            Assert.InRange(reading.Latitude, minLat, maxLat);
            Assert.InRange(reading.Longitude, minLng, maxLng);
        }
    }

    [Fact]
    public void Advance_ProducesHeadingsWithinCompassRange()
    {
        var device = new VirtualDevice(3, RouteCatalog.Routes[1], 0);
        var random = new Random(7);

        for (var i = 0; i < 200; i++)
        {
            var reading = device.Advance(random, 1.0);
            Assert.InRange(reading.HeadingDegrees, 0, 360);
        }
    }

    [Fact]
    public void Advance_EventuallyReportsIdleAndMovingReadings()
    {
        var device = new VirtualDevice(2, RouteCatalog.Routes[0], 0);
        var random = new Random(2024);

        var sawIdle = false;
        var sawMoving = false;

        for (var i = 0; i < 5000; i++)
        {
            var reading = device.Advance(random, 1.0);
            sawIdle |= reading.SpeedKph == 0;
            sawMoving |= reading.SpeedKph > 20;
        }

        Assert.True(sawIdle, "simulator never produced an idle reading");
        Assert.True(sawMoving, "simulator never produced a moving reading");
    }
}
