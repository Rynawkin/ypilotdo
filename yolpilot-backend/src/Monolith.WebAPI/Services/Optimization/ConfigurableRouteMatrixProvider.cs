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
        double depotLatitude,
        double depotLongitude,
        List<OptimizationStop> stops)
    {
        var preferredProvider = (_settings.MatrixProvider ?? "Osrm").Trim();

        if (preferredProvider.Equals("Google", StringComparison.OrdinalIgnoreCase))
        {
            return await TryGoogleThenOsrm(depotLatitude, depotLongitude, stops);
        }

        return await TryOsrmThenGoogle(depotLatitude, depotLongitude, stops);
    }

    private async Task<RouteMatrixResult> TryOsrmThenGoogle(
        double depotLatitude,
        double depotLongitude,
        List<OptimizationStop> stops)
    {
        try
        {
            return await _osrmProvider.BuildMatrixAsync(depotLatitude, depotLongitude, stops);
        }
        catch (Exception ex) when (_settings.EnableGoogleFallback)
        {
            _logger.LogWarning(ex, "OSRM matrix failed, falling back to Google Distance Matrix.");
            return await _googleProvider.BuildMatrixAsync(depotLatitude, depotLongitude, stops);
        }
    }

    private async Task<RouteMatrixResult> TryGoogleThenOsrm(
        double depotLatitude,
        double depotLongitude,
        List<OptimizationStop> stops)
    {
        try
        {
            return await _googleProvider.BuildMatrixAsync(depotLatitude, depotLongitude, stops);
        }
        catch (Exception ex) when (_osrmProvider.IsConfigured)
        {
            _logger.LogWarning(ex, "Google Distance Matrix failed, falling back to OSRM.");
            return await _osrmProvider.BuildMatrixAsync(depotLatitude, depotLongitude, stops);
        }
    }
}
