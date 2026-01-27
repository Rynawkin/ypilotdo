namespace Monolith.WebAPI.Requests;

public class OptimizeRouteRequest
{
    public string OptimizationMode { get; set; } = "distance";
    public bool AvoidTolls { get; set; } = false;
}