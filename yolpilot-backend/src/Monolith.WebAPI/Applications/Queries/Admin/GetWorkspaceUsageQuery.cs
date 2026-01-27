// src/Monolith.WebAPI/Applications/Queries/Admin/GetWorkspaceUsageQuery.cs
using MediatR;
using Microsoft.EntityFrameworkCore;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Responses.Admin;

namespace Monolith.WebAPI.Applications.Queries.Admin;

public record GetWorkspaceUsageQuery : IRequest<List<WorkspaceUsageResponse>>;

public class GetWorkspaceUsageQueryHandler(AppDbContext context) : IRequestHandler<GetWorkspaceUsageQuery, List<WorkspaceUsageResponse>>
{
    public async Task<List<WorkspaceUsageResponse>> Handle(GetWorkspaceUsageQuery request, CancellationToken cancellationToken)
    {
        var workspaces = await context.Workspaces
            .Select(w => new
            {
                w.Id,
                w.Name,
                w.Active,
                w.CreatedAt,
                UserCount = w.Users.Count(),
                DriverCount = w.Drivers.Count(),
                RouteCount = w.Routes.Count(),
                CustomerCount = w.Customers.Count(),
                LastActivity = w.UpdatedAt ?? DateTime.UtcNow
            })
            .ToListAsync(cancellationToken);

        return workspaces.Select(w => new WorkspaceUsageResponse
        {
            WorkspaceId = w.Id.ToString(),
            WorkspaceName = w.Name,
            Plan = GetPlanType(w.CreatedAt),
            Status = w.Active ? "active" : "inactive",
            UserCount = w.UserCount,
            DriverCount = w.DriverCount,
            RouteCount = w.RouteCount,
            CustomerCount = w.CustomerCount,
            LastActivity = w.LastActivity,
            MonthlyRevenue = GetMonthlyRevenue(GetPlanType(w.CreatedAt))
        }).OrderByDescending(w => w.LastActivity).ToList();
    }

    private static string GetPlanType(DateTime createdAt)
    {
        var daysOld = (DateTime.UtcNow - createdAt).Days;
        return daysOld <= 30 ? "trial" : "basic";
    }

    private static decimal GetMonthlyRevenue(string plan)
    {
        return plan switch
        {
            "trial" => 0,
            "basic" => 50,
            "premium" => 150,
            "enterprise" => 500,
            _ => 0
        };
    }
}