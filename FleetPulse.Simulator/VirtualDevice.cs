using FleetPulse.Core;
using FleetPulse.Core.Models;

namespace FleetPulse.Simulator;

/// <summary>
/// Tracks one simulated vehicle's progress along its assigned route and produces the next
/// telemetry reading each time <see cref="Advance"/> is called. Occasionally injects
/// speeding bursts, harsh braking/acceleration events, and idle stops so the rule engine
/// and dashboard have real signal to react to.
/// </summary>
public class VirtualDevice
{
    private readonly int _deviceId;
    private readonly RouteDefinition _route;

    /// <summary>Fraction (0..1) travelled along the current leg.</summary>
    private double _progress;
    private int _legIndex;
    private bool _idle;
    private int _idleTicksRemaining;
    private double _speedKph;

    public VirtualDevice(int deviceId, RouteDefinition route, double startProgress)
    {
        _deviceId = deviceId;
        _route = route;
        _legIndex = 0;
        _progress = Math.Clamp(startProgress, 0, 0.999);
        _speedKph = 45;
    }

    public TelemetryReading Advance(Random random, double tickSeconds)
    {
        var now = DateTimeOffset.UtcNow;

        if (_idle)
        {
            _idleTicksRemaining--;
            if (_idleTicksRemaining <= 0)
            {
                _idle = false;
            }

            var (idleLat, idleLng) = PositionOnLeg(_legIndex, _progress);
            return new TelemetryReading
            {
                DeviceId = _deviceId,
                TimestampUtc = now,
                Latitude = idleLat,
                Longitude = idleLng,
                SpeedKph = 0,
                HeadingDegrees = LegBearing(_legIndex),
                AccelerationG = 0,
                EngineRpm = 750,
                EngineOn = true
            };
        }

        if (random.NextDouble() < 0.004)
        {
            _idle = true;
            _idleTicksRemaining = random.Next(25, 45);
            _speedKph = 0;

            var (stopLat, stopLng) = PositionOnLeg(_legIndex, _progress);
            return new TelemetryReading
            {
                DeviceId = _deviceId,
                TimestampUtc = now,
                Latitude = stopLat,
                Longitude = stopLng,
                SpeedKph = 0,
                HeadingDegrees = LegBearing(_legIndex),
                AccelerationG = -0.35,
                EngineRpm = 750,
                EngineOn = true
            };
        }

        // Ease toward a new target speed rather than jumping, so the map motion looks natural.
        var targetSpeed = 35 + random.NextDouble() * 35;
        if (random.NextDouble() < 0.02)
        {
            targetSpeed += 25 + random.NextDouble() * 25;
        }

        _speedKph += (targetSpeed - _speedKph) * 0.25;

        AdvanceAlongRoute(_speedKph, tickSeconds);

        var (lat, lng) = PositionOnLeg(_legIndex, _progress);

        var acceleration = random.NextDouble() < 0.015
            ? (random.NextDouble() < 0.5
                ? -0.55 - random.NextDouble() * 0.45
                : 0.55 + random.NextDouble() * 0.35)
            : (random.NextDouble() - 0.5) * 0.12;

        return new TelemetryReading
        {
            DeviceId = _deviceId,
            TimestampUtc = now,
            Latitude = lat,
            Longitude = lng,
            SpeedKph = Math.Round(_speedKph, 1),
            HeadingDegrees = Math.Round(LegBearing(_legIndex), 1),
            AccelerationG = Math.Round(acceleration, 2),
            EngineRpm = Math.Round(900 + _speedKph * 20, 0),
            EngineOn = true
        };
    }

    /// <summary>
    /// Converts this tick's travelled distance into route progress. Legs vary in length
    /// (a downtown block vs. an airport highway run), so progress must be scaled by the
    /// actual leg distance — otherwise short legs get crossed in a single tick.
    /// </summary>
    private void AdvanceAlongRoute(double speedKph, double tickSeconds)
    {
        var remainingMeters = speedKph * 1000.0 / 3600.0 * tickSeconds;

        // Bounded so a degenerate zero-length leg can never spin forever.
        for (var hop = 0; hop < _route.Waypoints.Count && remainingMeters > 0; hop++)
        {
            var legMeters = LegLengthMeters(_legIndex);
            if (legMeters <= 0.01)
            {
                _legIndex = (_legIndex + 1) % _route.Waypoints.Count;
                _progress = 0;
                continue;
            }

            var metersLeftOnLeg = legMeters * (1 - _progress);

            if (remainingMeters < metersLeftOnLeg)
            {
                _progress += remainingMeters / legMeters;
                return;
            }

            remainingMeters -= metersLeftOnLeg;
            _legIndex = (_legIndex + 1) % _route.Waypoints.Count;
            _progress = 0;
        }
    }

    private double LegLengthMeters(int legIndex)
    {
        var from = _route.Waypoints[legIndex];
        var to = _route.Waypoints[(legIndex + 1) % _route.Waypoints.Count];
        return GeoMath.DistanceMeters(from.Latitude, from.Longitude, to.Latitude, to.Longitude);
    }

    private double LegBearing(int legIndex)
    {
        var from = _route.Waypoints[legIndex];
        var to = _route.Waypoints[(legIndex + 1) % _route.Waypoints.Count];
        return GeoMath.BearingDegrees(from.Latitude, from.Longitude, to.Latitude, to.Longitude);
    }

    private (double Latitude, double Longitude) PositionOnLeg(int legIndex, double progress)
    {
        var from = _route.Waypoints[legIndex];
        var to = _route.Waypoints[(legIndex + 1) % _route.Waypoints.Count];
        return (Lerp(from.Latitude, to.Latitude, progress), Lerp(from.Longitude, to.Longitude, progress));
    }

    private static double Lerp(double a, double b, double t) => a + (b - a) * t;
}
