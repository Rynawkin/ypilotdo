using System.Text.Json;
using Microsoft.Extensions.Options;
using Monolith.WebAPI.External.Google;

namespace Monolith.WebAPI.Services.Optimization;

public class OrderedRouteDetailsProvider : IOrderedRouteDetailsProvider
{
    private readonly GoogleApiService _googleApiService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly RoutingSettings _settings;
    private readonly ILogger<OrderedRouteDetailsProvider> _logger;

    public OrderedRouteDetailsProvider(
        GoogleApiService googleApiService,
        IHttpClientFactory httpClientFactory,
        IOptions<RoutingSettings> settings,
        ILogger<OrderedRouteDetailsProvider> logger)
    {
        _googleApiService = googleApiService;
        _httpClientFactory = httpClientFactory;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<OrderedRouteDetails> GetOrderedRouteAsync(
        string origin,
        string destination,
        List<string> orderedStops,
        bool avoidTolls,
        DateTime? departureTime = null)
    {
        if (!string.IsNullOrWhiteSpace(_settings.Osrm.BaseUrl))
        {
            try
            {
                return await GetOsrmRouteAsync(origin, destination, orderedStops);
            }
            catch (Exception ex) when (_settings.EnableGoogleFallback)
            {
                _logger.LogWarning(ex, "OSRM ordered route failed, falling back to Google Directions.");
            }
        }

        return await GetGoogleRouteAsync(origin, destination, orderedStops, avoidTolls, departureTime);
    }

    private async Task<OrderedRouteDetails> GetOsrmRouteAsync(
        string origin,
        string destination,
        List<string> orderedStops)
    {
        var coordinates = new List<string> { origin };
        coordinates.AddRange(orderedStops);
        coordinates.Add(destination);

        var coordinateSegment = string.Join(
            ";",
            coordinates.Select(ToOsrmCoordinate));

        var baseUrl = _settings.Osrm.BaseUrl.TrimEnd('/');
        var profile = string.IsNullOrWhiteSpace(_settings.Osrm.Profile) ? "driving" : _settings.Osrm.Profile;
        var requestUrl = $"{baseUrl}/route/v1/{profile}/{coordinateSegment}?overview=full&geometries=polyline&steps=false";

        var client = _httpClientFactory.CreateClient();
        using var response = await client.GetAsync(requestUrl);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        var payload = await JsonSerializer.DeserializeAsync<OsrmRouteResponse>(stream)
            ?? throw new InvalidOperationException("OSRM route returned an empty payload.");

        if (!string.Equals(payload.Code, "Ok", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"OSRM route returned '{payload.Code}'.");
        }

        var route = payload.Routes?.FirstOrDefault()
            ?? throw new InvalidOperationException("OSRM route response did not include a route.");

        return new OrderedRouteDetails
        {
            Legs = route.Legs?.Select(leg => new OrderedRouteLeg
            {
                DistanceMeters = (long)Math.Round(leg.Distance),
                DurationSeconds = (long)Math.Round(leg.Duration)
            }).ToList() ?? new List<OrderedRouteLeg>(),
            Polyline = route.Geometry,
            TotalDistanceKm = route.Distance / 1000d,
            TotalDurationMinutes = (int)Math.Round(route.Duration / 60d),
            ProviderName = "OSRM"
        };
    }

    private async Task<OrderedRouteDetails> GetGoogleRouteAsync(
        string origin,
        string destination,
        List<string> orderedStops,
        bool avoidTolls,
        DateTime? departureTime)
    {
        var coordinates = new List<string> { origin };
        coordinates.AddRange(orderedStops);
        coordinates.Add(destination);

        var maxCoordinatesPerRequest = Math.Max(3, _settings.Google.MaxRouteCoordinatesPerRequest);
        var allLegs = new List<OrderedRouteLeg>();
        string? polyline = null;
        var warnings = new List<string>();
        var currentDepartureTime = departureTime;
        var segmentCount = 0;

        for (var startIndex = 0; startIndex < coordinates.Count - 1;)
        {
            var takeCount = Math.Min(maxCoordinatesPerRequest, coordinates.Count - startIndex);
            var segment = coordinates.Skip(startIndex).Take(takeCount).ToList();

            if (segment.Count < 2)
            {
                break;
            }

            var segmentOrigin = segment.First();
            var segmentDestination = segment.Last();
            var segmentWaypoints = segment.Skip(1).Take(segment.Count - 2).ToList();

            var response = await _googleApiService.GetDirections(
                segmentOrigin,
                segmentDestination,
                segmentWaypoints,
                false,
                avoidTolls,
                currentDepartureTime);

            var route = response?.Routes?.FirstOrDefault()
                ?? throw new InvalidOperationException("Google Directions did not return a route.");

            if (segmentCount == 0 && startIndex == 0 && takeCount == coordinates.Count)
            {
                polyline = route.OverviewPolyline?.Points;
            }
            else
            {
                warnings.Add("Google route was chunked; overview polyline omitted.");
            }

            foreach (var leg in route.Legs ?? Enumerable.Empty<External.Google.Models.Leg>())
            {
                allLegs.Add(new OrderedRouteLeg
                {
                    DistanceMeters = leg.Distance?.Value ?? 0,
                    DurationSeconds = leg.Duration?.Value ?? 0
                });
            }

            var segmentTravelSeconds = route.Legs?.Sum(leg => leg.Duration?.Value ?? 0) ?? 0L;
            if (currentDepartureTime.HasValue)
            {
                currentDepartureTime = currentDepartureTime.Value.AddSeconds(segmentTravelSeconds);
            }

            segmentCount++;
            startIndex += segment.Count - 1;
        }

        return new OrderedRouteDetails
        {
            Legs = allLegs,
            Polyline = polyline,
            TotalDistanceKm = allLegs.Sum(leg => leg.DistanceMeters) / 1000d,
            TotalDurationMinutes = (int)Math.Round(allLegs.Sum(leg => leg.DurationSeconds) / 60d),
            ProviderName = "GoogleDirections",
            Warnings = warnings.Distinct().ToList()
        };
    }

    private static string ToOsrmCoordinate(string latLng)
    {
        var parts = latLng.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2)
        {
            throw new InvalidOperationException($"Invalid coordinate '{latLng}'.");
        }

        return $"{parts[1]},{parts[0]}";
    }

    private sealed class OsrmRouteResponse
    {
        public string? Code { get; set; }
        public List<OsrmRoute>? Routes { get; set; }
    }

    private sealed class OsrmRoute
    {
        public double Distance { get; set; }
        public double Duration { get; set; }
        public string? Geometry { get; set; }
        public List<OsrmRouteLeg>? Legs { get; set; }
    }

    private sealed class OsrmRouteLeg
    {
        public double Distance { get; set; }
        public double Duration { get; set; }
    }
}
