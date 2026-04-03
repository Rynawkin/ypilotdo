using System.Globalization;

namespace Monolith.WebAPI.Services.Optimization;

internal static class RouteMatrixEstimation
{
    public static List<(double Latitude, double Longitude)> BuildPoints(
        double depotLatitude,
        double depotLongitude,
        List<OptimizationStop> stops)
    {
        var points = new List<(double Latitude, double Longitude)>(stops.Count + 1)
        {
            (depotLatitude, depotLongitude)
        };

        points.AddRange(stops.Select(stop => (stop.Latitude, stop.Longitude)));
        return points;
    }

    public static string ToCoordinateString(double latitude, double longitude)
    {
        return string.Create(
            CultureInfo.InvariantCulture,
            $"{latitude},{longitude}");
    }

    public static long EstimateDistanceMeters(
        (double Latitude, double Longitude) from,
        (double Latitude, double Longitude) to)
    {
        const double earthRadiusKm = 6371d;

        var dLat = DegreesToRadians(to.Latitude - from.Latitude);
        var dLng = DegreesToRadians(to.Longitude - from.Longitude);
        var lat1 = DegreesToRadians(from.Latitude);
        var lat2 = DegreesToRadians(to.Latitude);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Sin(dLng / 2) * Math.Sin(dLng / 2) * Math.Cos(lat1) * Math.Cos(lat2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        var distanceKm = earthRadiusKm * c;
        return (long)Math.Round(distanceKm * 1000d);
    }

    public static long EstimateDurationSeconds(
        (double Latitude, double Longitude) from,
        (double Latitude, double Longitude) to,
        double averageSpeedKph = 35d)
    {
        var distanceMeters = EstimateDistanceMeters(from, to);
        var distanceKm = distanceMeters / 1000d;
        var hours = distanceKm / averageSpeedKph;
        return (long)Math.Max(1d, Math.Round(hours * 3600d));
    }

    private static double DegreesToRadians(double degrees)
    {
        return degrees * Math.PI / 180d;
    }
}
