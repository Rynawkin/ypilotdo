// src/Monolith.WebAPI/Applications/Queries/Admin/GetWorkspaceStatsQuery.cs
using MediatR;
using Microsoft.EntityFrameworkCore;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Responses.Admin;

namespace Monolith.WebAPI.Applications.Queries.Admin;

public record GetWorkspaceStatsQuery : IRequest<WorkspaceStatsResponse>;

public class GetWorkspaceStatsQueryHandler(AppDbContext context) : IRequestHandler<GetWorkspaceStatsQuery, WorkspaceStatsResponse>
{
    public async Task<WorkspaceStatsResponse> Handle(GetWorkspaceStatsQuery request, CancellationToken cancellationToken)
    {
        var totalWorkspaces = await context.Workspaces.CountAsync(cancellationToken);
        var activeWorkspaces = await context.Workspaces.CountAsync(w => w.Active, cancellationToken);
        
        // Trial workspace'leri (30 günden yeni olanlar)
        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
        var trialWorkspaces = await context.Workspaces
            .CountAsync(w => w.CreatedAt >= thirtyDaysAgo, cancellationToken);
        
        var totalUsers = await context.Users.CountAsync(cancellationToken);
        var totalRoutes = await context.Routes.CountAsync(cancellationToken);
        
        // Revenue hesaplaması (basit bir hesaplama - gerçek subscription sistemi yoksa)
        var monthlyRevenuePerWorkspace = 100; // Ortalama aylık gelir
        var totalRevenue = activeWorkspaces * monthlyRevenuePerWorkspace;
        
        return new WorkspaceStatsResponse
        {
            TotalWorkspaces = totalWorkspaces,
            ActiveWorkspaces = activeWorkspaces,
            TrialWorkspaces = trialWorkspaces,
            TotalRevenue = totalRevenue,
            TotalUsers = totalUsers,
            TotalRoutes = totalRoutes
        };
    }
}