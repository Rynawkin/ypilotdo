// src/Monolith.WebAPI/Responses/Admin/AdminResponses.cs

#nullable enable
using Monolith.WebAPI.Data.Workspace;
using Monolith.WebAPI.Services.Subscription;


namespace Monolith.WebAPI.Responses.Admin;

public class WorkspaceStatsResponse
{
    public int TotalWorkspaces { get; set; }
    public int ActiveWorkspaces { get; set; }
    public int TrialWorkspaces { get; set; }
    public decimal TotalRevenue { get; set; }
    public int TotalUsers { get; set; }
    public int TotalRoutes { get; set; }
}

public class WorkspaceUsageResponse
{
    public string WorkspaceId { get; set; } = string.Empty;
    public string WorkspaceName { get; set; } = string.Empty;
    public string Plan { get; set; } = "trial";
    public string Status { get; set; } = "active";
    public int UserCount { get; set; }
    public int DriverCount { get; set; }
    public int RouteCount { get; set; }
    public int CustomerCount { get; set; }
    public DateTime LastActivity { get; set; }
    public decimal MonthlyRevenue { get; set; }
}

public class WorkspaceListResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string DistanceUnit { get; set; } = "km";
    public string Currency { get; set; } = "TRY";
    public string TimeZone { get; set; } = "Europe/Istanbul";
    public string Language { get; set; } = "TR";
    public TimeSpan DefaultServiceTime { get; set; }
    public int MaximumDriverCount { get; set; }
    public bool Active { get; set; }
    public DateTime CreatedAt { get; set; }
    public SubscriptionResponse? Subscription { get; set; }
}

public class WorkspaceDetailResponse : WorkspaceListResponse
{
    public int UserCount { get; set; }
    public int DriverCount { get; set; }
    public int VehicleCount { get; set; }
    public int CustomerCount { get; set; }
    public int RouteCount { get; set; }
    public int DepotCount { get; set; }
    public DateTime? LastActivity { get; set; }
    public PlanType PlanType { get; set; }
    public DateTime? PlanStartDate { get; set; }
    public DateTime? PlanEndDate { get; set; }
    public int CurrentMonthStops { get; set; }
    public decimal CurrentMonthAdditionalCharges { get; set; }
    public int CurrentMonthWhatsAppMessages { get; set; }
    public DateTime LastStopResetDate { get; set; }
    public PlanLimits PlanLimits { get; set; }
}

public class SubscriptionResponse
{
    public string Plan { get; set; } = "trial";
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Status { get; set; } = "active";
    public int MaxDrivers { get; set; }
    public int MaxRoutes { get; set; }
    public int MaxCustomers { get; set; }
}

public class UpdateWorkspaceStatusRequest
{
    public bool Active { get; set; }
}
