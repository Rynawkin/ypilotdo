namespace Monolith.WebAPI.Data.Journeys;

public class RouteOptimizationJob : BaseEntity
{
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public int RouteId { get; set; }
    public Route Route { get; set; }
    public int WorkspaceId { get; set; }
    public Guid RequestedByUserId { get; set; }
    public string OptimizationMode { get; set; } = "distance";
    public bool AvoidTolls { get; set; }
    public bool PreserveOrder { get; set; }
    public bool IsTimeDeviationOptimization { get; set; }
    public string Status { get; set; } = RouteOptimizationJobStatus.Pending;
    public string Message { get; set; }
    public string ResultJson { get; set; }
    public string Error { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public static class RouteOptimizationJobStatus
{
    public const string Pending = "pending";
    public const string Running = "running";
    public const string Completed = "completed";
    public const string Failed = "failed";
}
