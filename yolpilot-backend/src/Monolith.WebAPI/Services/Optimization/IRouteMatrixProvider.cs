namespace Monolith.WebAPI.Services.Optimization;

public interface IRouteMatrixProvider
{
    Task<RouteMatrixResult> BuildMatrixAsync(
        double originLatitude,
        double originLongitude,
        List<OptimizationStop> stops,
        double? endLatitude = null,
        double? endLongitude = null);
}

public class RouteMatrixResult
{
    public required long[,] DistanceMatrix { get; init; }
    public required long[,] TimeMatrix { get; init; }
    public required string ProviderName { get; init; }
    public List<string> Warnings { get; init; } = new();
}
