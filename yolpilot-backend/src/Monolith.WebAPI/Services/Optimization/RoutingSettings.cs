namespace Monolith.WebAPI.Services.Optimization;

public class RoutingSettings
{
    public const string SectionName = "Routing";

    public string MatrixProvider { get; set; } = "Osrm";
    public bool EnableGoogleFallback { get; set; } = true;
    public GoogleRoutingSettings Google { get; set; } = new();
    public OsrmRoutingSettings Osrm { get; set; } = new();
}

public class GoogleRoutingSettings
{
    public int MaxBatchSize { get; set; } = 10;
    public int MaxRouteCoordinatesPerRequest { get; set; } = 25;
}

public class OsrmRoutingSettings
{
    public string BaseUrl { get; set; } = string.Empty;
    public string Profile { get; set; } = "driving";
    public double? FallbackSpeedKph { get; set; }
}
