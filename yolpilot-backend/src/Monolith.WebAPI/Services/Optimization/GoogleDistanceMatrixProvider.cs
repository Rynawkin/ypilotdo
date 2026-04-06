using Microsoft.Extensions.Options;
using Monolith.WebAPI.External.Google;

namespace Monolith.WebAPI.Services.Optimization;

public class GoogleDistanceMatrixProvider : IRouteMatrixProvider
{
    private readonly GoogleApiService _googleApiService;
    private readonly ILogger<GoogleDistanceMatrixProvider> _logger;
    private readonly RoutingSettings _settings;

    public GoogleDistanceMatrixProvider(
        GoogleApiService googleApiService,
        IOptions<RoutingSettings> settings,
        ILogger<GoogleDistanceMatrixProvider> logger)
    {
        _googleApiService = googleApiService;
        _logger = logger;
        _settings = settings.Value;
    }

    public async Task<RouteMatrixResult> BuildMatrixAsync(
        double originLatitude,
        double originLongitude,
        List<OptimizationStop> stops,
        double? endLatitude = null,
        double? endLongitude = null)
    {
        var points = RouteMatrixEstimation.BuildPoints(originLatitude, originLongitude, stops, endLatitude, endLongitude);
        var coordinateStrings = points
            .Select(point => RouteMatrixEstimation.ToCoordinateString(point.Latitude, point.Longitude))
            .ToList();

        var size = coordinateStrings.Count;
        var distanceMatrix = new long[size, size];
        var timeMatrix = new long[size, size];
        var batchSize = Math.Max(1, _settings.Google.MaxBatchSize);

        _logger.LogInformation(
            "Building Google Distance Matrix for {PointCount} points with batch size {BatchSize}",
            size,
            batchSize);

        for (var originStart = 0; originStart < size; originStart += batchSize)
        {
            var originBatch = coordinateStrings.Skip(originStart).Take(batchSize).ToList();

            for (var destinationStart = 0; destinationStart < size; destinationStart += batchSize)
            {
                var destinationBatch = coordinateStrings.Skip(destinationStart).Take(batchSize).ToList();

                var response = await _googleApiService.GetDistanceMatrix(originBatch, destinationBatch);

                if (!string.Equals(response?.Status, "OK", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        $"Google Distance Matrix returned status '{response?.Status ?? "null"}'.");
                }

                for (var i = 0; i < originBatch.Count; i++)
                {
                    var row = response.Rows.ElementAtOrDefault(i)
                        ?? throw new InvalidOperationException("Google Distance Matrix row missing.");

                    for (var j = 0; j < destinationBatch.Count; j++)
                    {
                        var element = row.Elements?.ElementAtOrDefault(j)
                            ?? throw new InvalidOperationException("Google Distance Matrix element missing.");

                        if (!string.Equals(element.Status, "OK", StringComparison.OrdinalIgnoreCase))
                        {
                            throw new InvalidOperationException(
                                $"Google Distance Matrix cell [{originStart + i},{destinationStart + j}] returned '{element.Status}'.");
                        }

                        distanceMatrix[originStart + i, destinationStart + j] = element.Distance?.Value ?? 0;
                        timeMatrix[originStart + i, destinationStart + j] = element.Duration?.Value ?? 0;
                    }
                }
            }
        }

        return new RouteMatrixResult
        {
            DistanceMatrix = distanceMatrix,
            TimeMatrix = timeMatrix,
            ProviderName = "GoogleDistanceMatrix"
        };
    }
}
