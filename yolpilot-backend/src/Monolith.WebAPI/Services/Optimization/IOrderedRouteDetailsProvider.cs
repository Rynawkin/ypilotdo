namespace Monolith.WebAPI.Services.Optimization;

public interface IOrderedRouteDetailsProvider
{
    Task<OrderedRouteDetails> GetOrderedRouteAsync(
        string origin,
        string destination,
        List<string> orderedStops,
        bool avoidTolls,
        DateTime? departureTime = null);
}

public class OrderedRouteDetails
{
    public required List<OrderedRouteLeg> Legs { get; init; }
    public string? Polyline { get; init; }
    public double TotalDistanceKm { get; init; }
    public int TotalDurationMinutes { get; init; }
    public required string ProviderName { get; init; }
    public List<string> Warnings { get; init; } = new();
}

public class OrderedRouteLeg
{
    public long DistanceMeters { get; init; }
    public long DurationSeconds { get; init; }
}
