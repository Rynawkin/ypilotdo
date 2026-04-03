using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Monolith.WebAPI.Services.Optimization;

public class OsrmRouteMatrixProvider
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<OsrmRouteMatrixProvider> _logger;
    private readonly RoutingSettings _settings;

    public OsrmRouteMatrixProvider(
        IHttpClientFactory httpClientFactory,
        IOptions<RoutingSettings> settings,
        ILogger<OsrmRouteMatrixProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _settings = settings.Value;
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_settings.Osrm.BaseUrl);

    public async Task<RouteMatrixResult> BuildMatrixAsync(
        double depotLatitude,
        double depotLongitude,
        List<OptimizationStop> stops)
    {
        if (!IsConfigured)
        {
            throw new InvalidOperationException("OSRM base URL is not configured.");
        }

        var points = RouteMatrixEstimation.BuildPoints(depotLatitude, depotLongitude, stops);
        var coordinateSegment = string.Join(
            ";",
            points.Select(point => $"{point.Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture)},{point.Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}"));

        var baseUrl = _settings.Osrm.BaseUrl.TrimEnd('/');
        var profile = string.IsNullOrWhiteSpace(_settings.Osrm.Profile) ? "driving" : _settings.Osrm.Profile;
        var requestUrl = $"{baseUrl}/table/v1/{profile}/{coordinateSegment}?annotations=distance,duration";

        if (_settings.Osrm.FallbackSpeedKph is > 0)
        {
            var fallbackMetersPerSecond = _settings.Osrm.FallbackSpeedKph.Value / 3.6d;
            requestUrl += $"&fallback_speed={fallbackMetersPerSecond.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
        }

        _logger.LogInformation("Requesting OSRM matrix for {PointCount} points", points.Count);

        var client = _httpClientFactory.CreateClient();
        using var response = await client.GetAsync(requestUrl);
        response.EnsureSuccessStatusCode();

        await using var responseStream = await response.Content.ReadAsStreamAsync();
        var payload = await JsonSerializer.DeserializeAsync<OsrmTableResponse>(responseStream)
            ?? throw new InvalidOperationException("OSRM returned an empty payload.");

        if (!string.Equals(payload.Code, "Ok", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"OSRM returned '{payload.Code}'.");
        }

        if (payload.Durations == null || payload.Distances == null)
        {
            throw new InvalidOperationException("OSRM matrix is missing durations or distances.");
        }

        var size = points.Count;
        var distanceMatrix = new long[size, size];
        var timeMatrix = new long[size, size];
        var warnings = new List<string>();

        for (var i = 0; i < size; i++)
        {
            var durationRow = payload.Durations.ElementAtOrDefault(i)
                ?? throw new InvalidOperationException($"OSRM duration row {i} is missing.");
            var distanceRow = payload.Distances.ElementAtOrDefault(i)
                ?? throw new InvalidOperationException($"OSRM distance row {i} is missing.");

            for (var j = 0; j < size; j++)
            {
                var durationValue = durationRow.ElementAtOrDefault(j);
                var distanceValue = distanceRow.ElementAtOrDefault(j);

                if (!durationValue.HasValue || !distanceValue.HasValue)
                {
                    throw new InvalidOperationException(
                        $"OSRM matrix contains an empty cell at [{i},{j}].");
                }

                timeMatrix[i, j] = (long)Math.Round(durationValue.Value);
                distanceMatrix[i, j] = (long)Math.Round(distanceValue.Value);
            }
        }

        if (payload.FallbackSpeedCells?.Count > 0)
        {
            warnings.Add($"{payload.FallbackSpeedCells.Count} OSRM matrix cell estimated by fallback speed.");
        }

        return new RouteMatrixResult
        {
            DistanceMatrix = distanceMatrix,
            TimeMatrix = timeMatrix,
            ProviderName = "OSRM",
            Warnings = warnings
        };
    }

    private sealed class OsrmTableResponse
    {
        public string? Code { get; set; }
        public List<List<double?>>? Durations { get; set; }
        public List<List<double?>>? Distances { get; set; }
        public List<List<int>>? FallbackSpeedCells { get; set; }
    }
}
