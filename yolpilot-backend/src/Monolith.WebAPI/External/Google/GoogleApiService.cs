using Flurl;
using Flurl.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Monolith.WebAPI.External.Google.Models;
using Monolith.WebAPI.Services.Cache;
using System.Text.Json;

namespace Monolith.WebAPI.External.Google;

public class GoogleApiService
{
    private readonly string ApiKey;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GoogleApiService> _logger;
    private readonly IGoogleApiCacheService _cacheService;
    private const string BaseUrl = "https://maps.googleapis.com/maps/api";
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(30);

    public GoogleApiService(
        IConfiguration configuration,
        IGoogleApiCacheService cacheService,
        ILogger<GoogleApiService> logger = null)
    {
        _configuration = configuration;
        _logger = logger;
        _cacheService = cacheService;
        ApiKey = _configuration["GoogleMaps:ApiKey"] ?? throw new Exception("Google Maps API Key not found in configuration!");

        // API Key'i logla (son 4 karakteri göster güvenlik için)
        if (_logger != null)
        {
            var maskedKey = ApiKey.Length > 4 ? $"...{ApiKey.Substring(ApiKey.Length - 4)}" : "****";
            _logger.LogInformation($"Google API initialized with key ending: {maskedKey}");
        }
    }

    public async Task<PlaceSearchResponse> SearchPlaces(string query, string location = null, int radius = 5000)
    {
        try
        {
            var url = $"{BaseUrl}/place/textsearch/json"
                .SetQueryParam("query", query)
                .SetQueryParam("key", ApiKey);

            if (!string.IsNullOrEmpty(location))
                url = url.SetQueryParam("location", location)
                         .SetQueryParam("radius", radius);

            var response = await url.WithTimeout(RequestTimeout).GetJsonAsync<PlaceSearchResponse>();
            return response;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Google Places API error");
            throw new Exception($"Google Places API error: {ex.Message}", ex);
        }
    }

    public async Task<PlaceDetailsResponse> GetPlaceDetails(string placeId)
    {
        try
        {
            var response = await $"{BaseUrl}/place/details/json"
                .SetQueryParam("place_id", placeId)
                .SetQueryParam("fields", "name,formatted_address,geometry,formatted_phone_number,website,opening_hours,rating")
                .SetQueryParam("key", ApiKey)
                .WithTimeout(RequestTimeout)
                .GetJsonAsync<PlaceDetailsResponse>();

            return response;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Google Place Details API error");
            throw new Exception($"Google Place Details API error: {ex.Message}", ex);
        }
    }

    public async Task<GeocodingResponse> GeocodeAddress(string address)
    {
        try
        {
            var response = await $"{BaseUrl}/geocode/json"
                .SetQueryParam("address", address)
                .SetQueryParam("key", ApiKey)
                .WithTimeout(RequestTimeout)
                .GetJsonAsync<GeocodingResponse>();

            return response;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Google Geocoding API error");
            throw new Exception($"Google Geocoding API error: {ex.Message}", ex);
        }
    }

    public async Task<DirectionsResponse> GetDirections(string origin, string destination, List<string> waypoints = null, bool optimize = false, bool avoidTolls = false, DateTime? departureTime = null)
    {
        // PERFORMANCE: Cache directions for 24 hours to reduce API costs
        var cacheKey = _cacheService.GenerateCacheKey(
            "directions",
            origin,
            destination,
            waypoints != null ? string.Join(",", waypoints) : "",
            optimize.ToString(),
            avoidTolls.ToString(),
            departureTime?.ToString("o") ?? ""
        );

        return await _cacheService.GetOrCreateAsync(cacheKey, async () =>
        {
        try
        {
            // Debug log - gönderilen parametreleri logla
            _logger?.LogInformation($"=== GOOGLE DIRECTIONS API REQUEST ===");
            _logger?.LogInformation($"Origin: {origin}");
            _logger?.LogInformation($"Destination: {destination}");
            _logger?.LogInformation($"Waypoints count: {waypoints?.Count ?? 0}");
            if (waypoints != null && waypoints.Any())
            {
                for (int i = 0; i < waypoints.Count; i++)
                {
                    _logger?.LogInformation($"Waypoint {i + 1}: {waypoints[i]}");
                }
            }
            _logger?.LogInformation($"Optimize: {optimize}");
            _logger?.LogInformation($"Avoid tolls: {avoidTolls}");

            var url = $"{BaseUrl}/directions/json"
                .SetQueryParam("origin", origin)
                .SetQueryParam("destination", destination)
                .SetQueryParam("key", ApiKey)
                .SetQueryParam("units", "metric")  // Metrik birim kullan
                .SetQueryParam("mode", "driving"); // Sürüş modu

            if (avoidTolls)
            {
                url = url.SetQueryParam("avoid", "tolls");
            }

            // ⭐ Traffic-aware routing için departure_time ekle
            if (departureTime.HasValue)
            {
                var unixTimestamp = ((DateTimeOffset)departureTime.Value).ToUnixTimeSeconds();
                url = url.SetQueryParam("departure_time", unixTimestamp);
                _logger?.LogInformation($"Using traffic-aware routing with departure time: {departureTime.Value:yyyy-MM-dd HH:mm:ss} (Unix: {unixTimestamp})");
            }

            if (waypoints != null && waypoints.Any())
            {
                var waypointsStr = optimize ? "optimize:true|" : "";
                waypointsStr += string.Join("|", waypoints);
                url = url.SetQueryParam("waypoints", waypointsStr);
                _logger?.LogInformation($"Waypoints parameter: {waypointsStr}");
            }

            // URL'yi logla (API key'i maskeleyerek)
            var urlString = url.ToString();
            var maskedUrl = urlString.Replace(ApiKey, "***API_KEY***");
            _logger?.LogInformation($"Request URL: {maskedUrl}");

            var response = await url.WithTimeout(RequestTimeout).GetJsonAsync<DirectionsResponse>();
            
            // Response'u logla
            _logger?.LogInformation($"=== GOOGLE DIRECTIONS API RESPONSE ===");
            _logger?.LogInformation($"Status: {response?.Status}");
            _logger?.LogInformation($"Routes count: {response?.Routes?.Count ?? 0}");
            
            if (response?.Routes?.FirstOrDefault() != null)
            {
                var route = response.Routes.First();
                _logger?.LogInformation($"Total legs: {route.Legs?.Count ?? 0}");
                
                // ✅ ÖNEMLİ: WaypointOrder'ı Route'dan DirectionsResponse'a kopyala
                if (route.WaypointOrder != null && route.WaypointOrder.Any())
                {
                    response.WaypointOrder = route.WaypointOrder;
                    _logger?.LogInformation($"Waypoint order found in route: [{string.Join(", ", route.WaypointOrder)}]");
                }
                else
                {
                    _logger?.LogWarning("No waypoint order found in route!");
                    response.WaypointOrder = new List<int>();
                }
                
                double totalDistance = 0;
                int totalDuration = 0;
                foreach (var leg in route.Legs ?? new List<Leg>())
                {
                    if (leg?.Distance != null && leg?.Duration != null)
                    {
                        totalDistance += leg.Distance.Value / 1000.0;
                        totalDuration += (int)(leg.Duration.Value / 60);
                    }
                }
                _logger?.LogInformation($"Total distance: {totalDistance:F2} km");
                _logger?.LogInformation($"Total duration: {totalDuration} minutes");
            }
            else
            {
                _logger?.LogWarning("No routes returned from Google Directions API");
                
                // Error message varsa logla
                if (!string.IsNullOrEmpty(response?.ErrorMessage))
                {
                    _logger?.LogError($"Google API Error: {response.ErrorMessage}");
                }
                
                // Geocoded waypoints'i kontrol et
                if (response?.GeocodedWaypoints != null)
                {
                    for (int i = 0; i < response.GeocodedWaypoints.Count; i++)
                    {
                        var wp = response.GeocodedWaypoints[i];
                        _logger?.LogInformation($"Waypoint {i}: Status={wp?.GeocoderStatus}, PlaceId={wp?.PlaceId}");
                    }
                }
            }
            
            return response;
        }
        catch (FlurlHttpException httpEx)
        {
            // HTTP hatalarını detaylı logla
            var responseBody = await httpEx.GetResponseStringAsync();
            _logger?.LogError($"Google Directions API HTTP Error: Status={httpEx.StatusCode}, Body={responseBody}");
            throw new Exception($"Google Directions API error: {httpEx.Message}\nResponse: {responseBody}", httpEx);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Google Directions API error");
            throw new Exception($"Google Directions API error: {ex.Message}", ex);
        }
        }); // End of cache wrapper
    }

    public async Task<DistanceMatrixResponse> GetDistanceMatrix(List<string> origins, List<string> destinations)
    {
        // PERFORMANCE: Cache distance matrix for 24 hours to reduce API costs
        var cacheKey = _cacheService.GenerateCacheKey(
            "distancematrix",
            string.Join(",", origins),
            string.Join(",", destinations)
        );

        return await _cacheService.GetOrCreateAsync(cacheKey, async () =>
        {
        try
        {
            var response = await $"{BaseUrl}/distancematrix/json"
                .SetQueryParam("origins", string.Join("|", origins))
                .SetQueryParam("destinations", string.Join("|", destinations))
                .SetQueryParam("key", ApiKey)
                .SetQueryParam("units", "metric")
                .WithTimeout(RequestTimeout)
                .GetJsonAsync<DistanceMatrixResponse>();

            return response;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Google Distance Matrix API error");
            throw new Exception($"Google Distance Matrix API error: {ex.Message}", ex);
        }
        }); // End of cache wrapper
    }

    public async Task<AutocompleteResponse> GetAutocomplete(string input, string location = null, int radius = 50000)
    {
        try
        {
            var url = $"{BaseUrl}/place/autocomplete/json"
                .SetQueryParam("input", input)
                .SetQueryParam("key", ApiKey);

            if (!string.IsNullOrEmpty(location))
                url = url.SetQueryParam("location", location)
                         .SetQueryParam("radius", radius);

            var response = await url.WithTimeout(RequestTimeout).GetJsonAsync<AutocompleteResponse>();
            return response;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Google Autocomplete API error");
            throw new Exception($"Google Autocomplete API error: {ex.Message}", ex);
        }
    }
}
