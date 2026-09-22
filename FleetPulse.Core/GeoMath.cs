namespace FleetPulse.Core;

public static class GeoMath
{
    private const double EarthRadiusMeters = 6_371_000;

    public static double ToRadians(double degrees) => degrees * Math.PI / 180.0;

    public static double ToDegrees(double radians) => radians * 180.0 / Math.PI;

    /// <summary>Great-circle distance between two WGS84 points, in metres.</summary>
    public static double DistanceMeters(double lat1, double lng1, double lat2, double lng2)
    {
        var dLat = ToRadians(lat2 - lat1);
        var dLng = ToRadians(lng2 - lng1);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLng / 2) * Math.Sin(dLng / 2);

        return EarthRadiusMeters * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    /// <summary>Initial bearing from one point to another, in degrees clockwise from north.</summary>
    public static double BearingDegrees(double lat1, double lng1, double lat2, double lng2)
    {
        var phi1 = ToRadians(lat1);
        var phi2 = ToRadians(lat2);
        var dLambda = ToRadians(lng2 - lng1);

        var y = Math.Sin(dLambda) * Math.Cos(phi2);
        var x = Math.Cos(phi1) * Math.Sin(phi2) - Math.Sin(phi1) * Math.Cos(phi2) * Math.Cos(dLambda);

        return (ToDegrees(Math.Atan2(y, x)) + 360) % 360;
    }
}
