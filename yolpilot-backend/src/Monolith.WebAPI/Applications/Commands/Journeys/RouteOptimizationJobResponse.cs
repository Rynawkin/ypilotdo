using System.Text.Json;

namespace Monolith.WebAPI.Applications.Commands.Journeys;

public class RouteOptimizationJobResponse
{
    public Guid JobId { get; set; }
    public int RouteId { get; set; }
    public string Status { get; set; }
    public string Message { get; set; }
}

public class RouteOptimizationJobStatusResponse
{
    public Guid JobId { get; set; }
    public int RouteId { get; set; }
    public string Status { get; set; }
    public string Message { get; set; }
    public JsonElement? Result { get; set; }
}
