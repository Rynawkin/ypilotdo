using Microsoft.Extensions.Options;

namespace Monolith.WebAPI.Services.Optimization;

public class ConfigurableRouteMatrixProvider : IRouteMatrixProvider
{
    private readonly GoogleDistanceMatrixProvider _googleProvider;
    private readonly OsrmRouteMatrixProvider _osrmProvider;
    private readonly RoutingSettings _settings;
    private readonly ILogger<ConfigurableRouteMatrixProvider> _logger;

    public ConfigurableRouteMatrixProvider(
        GoogleDistanceMatrixProvider googleProvider,
        OsrmRouteMatrixProvider osrmProvider,
        IOptions<RoutingSettings> settings,
        ILogger<ConfigurableRouteMatrixProvider> logger)
    {
        _googleProvider = googleProvider;
        _osrmProvider = osrmProvider;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<RouteMatrixResult> BuildMatrixAsync(
        double originLatitude,
        double originLongitude,
        List<OptimizationStop> stops,
        double? endLatitude = null,
        double? endLongitude = null)
    {
        var preferredProvider = (_settings.MatrixProvider ?? "Osrm").Trim();

        if (preferredProvider.Equals("Google", StringComparison.OrdinalIgnoreCase))
        {
            return await TryGoogleThenOsrm(originLatitude, originLongitude, stops, endLatitude, endLongitude);
        }

        return await TryOsrmThenGoogle(originLatitude, originLongitude, stops, endLatitude, endLongitude);
    }

    private async Task<RouteMatrixResult> TryOsrmThenGoogle(
        double originLatitude,
        double originLongitude,
        List<OptimizationStop> stops,
        double? endLatitude,
        double? endLongitude)
    {
        try
        {
            return await _osrmProvider.BuildMatrixAsync(originLatitude, originLongitude, stops, endLatitude, endLongitude);
        }
        catch (Exception ex) when (_settings.EnableGoogleFallback)
        {
            _logger.LogWarning(ex, "OSRM matrix failed, falling back to Google Distance Matrix.");
            return await _googleProvider.BuildMatrixAsync(originLatitude, originLongitude, stops, endLatitude, endLongitude);
        }
    }

    private async Task<RouteMatrixResult> TryGoogleThenOsrm(
        double originLatitude,
        double originLongitude,
        List<OptimizationStop> stops,
        double? endLatitude,
        double? endLongitude)
    {
        try
        {
            return await _googleProvider.BuildMatrixAsync(originLatitude, originLongitude, stops, endLatitude, endLongitude);
        }
        catch (Exception ex) when (_osrmProvider.IsConfigured)
        {
            _logger.LogWarning(ex, "Google Distance Matrix failed, falling back to OSRM.");
            return await _osrmProvider.BuildMatrixAsync(originLatitude, originLongitude, stops, endLatitude, endLongitude);
        }
    }
}
