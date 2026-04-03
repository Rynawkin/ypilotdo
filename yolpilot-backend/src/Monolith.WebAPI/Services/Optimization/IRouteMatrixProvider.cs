namespace Monolith.WebAPI.Services.Optimization;

public interface IRouteMatrixProvider
{
    Task<RouteMatrixResult> BuildMatrixAsync(
        double depotLatitude,
        double depotLongitude,
        List<OptimizationStop> stops);
}

public class RouteMatrixResult
{
    public required long[,] DistanceMatrix { get; init; }
    public required long[,] TimeMatrix { get; init; }
    public required string ProviderName { get; init; }
    public List<string> Warnings { get; init; } = new();
}
