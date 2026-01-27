// src/Monolith.WebAPI/Applications/Queries/Admin/GetWorkspaceByIdQuery.cs
using MediatR;
using Microsoft.EntityFrameworkCore;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Data.Workspace;
using Monolith.WebAPI.Responses.Admin;
using Monolith.WebAPI.Services.Subscription;

namespace Monolith.WebAPI.Applications.Queries.Admin;

public record GetWorkspaceByIdQuery(int WorkspaceId) : IRequest<WorkspaceDetailResponse>;

public class GetWorkspaceByIdQueryHandler : IRequestHandler<GetWorkspaceByIdQuery, WorkspaceDetailResponse>
{
    private readonly AppDbContext _context;
    private readonly ISubscriptionService _subscriptionService;

    public GetWorkspaceByIdQueryHandler(AppDbContext context, ISubscriptionService subscriptionService)
    {
        _context = context;
        _subscriptionService = subscriptionService;
    }

    public async Task<WorkspaceDetailResponse> Handle(GetWorkspaceByIdQuery request, CancellationToken cancellationToken)
    {
        var workspace = await _context.Workspaces
            .Include(w => w.Users)
            .Include(w => w.Drivers)
            .Include(w => w.Vehicles)
            .Include(w => w.Customers)
            .Include(w => w.Routes)
            .Include(w => w.Depots)
            .FirstOrDefaultAsync(w => w.Id == request.WorkspaceId, cancellationToken);

        if (workspace == null)
            throw new KeyNotFoundException($"Workspace with ID {request.WorkspaceId} not found");

        var planLimits = _subscriptionService.GetPlanLimits(workspace.PlanType);

        return new WorkspaceDetailResponse
        {
            Id = workspace.Id,
            Name = workspace.Name,
            Email = workspace.Email,
            PhoneNumber = workspace.PhoneNumber,
            DistanceUnit = workspace.DistanceUnit,
            Currency = workspace.Currency,
            TimeZone = workspace.TimeZone,
            Language = workspace.Language,
            DefaultServiceTime = workspace.DefaultServiceTime,
            MaximumDriverCount = workspace.MaximumDriverCount,
            Active = workspace.Active,
            CreatedAt = workspace.CreatedAt,
            UserCount = workspace.Users.Count,
            DriverCount = workspace.Drivers.Count,
            VehicleCount = workspace.Vehicles.Count,
            CustomerCount = workspace.Customers.Count,
            RouteCount = workspace.Routes.Count,
            DepotCount = workspace.Depots.Count,
            LastActivity = workspace.UpdatedAt,
            
            // Plan bilgileri
            PlanType = workspace.PlanType,
            PlanStartDate = workspace.PlanStartDate,
            PlanEndDate = workspace.PlanEndDate,
            
            // Kullanım bilgileri
            CurrentMonthStops = workspace.CurrentMonthStops,
            CurrentMonthAdditionalCharges = workspace.CurrentMonthAdditionalCharges,
            CurrentMonthWhatsAppMessages = workspace.CurrentMonthWhatsAppMessages,
            LastStopResetDate = workspace.LastStopResetDate,
            
            // Plan limitleri
            PlanLimits = planLimits,
            
            // Legacy subscription response (geriye uyumluluk için)
            Subscription = new SubscriptionResponse
            {
                Plan = workspace.PlanType.ToString(),
                StartDate = workspace.PlanStartDate ?? workspace.CreatedAt,
                EndDate = workspace.PlanEndDate ?? workspace.CreatedAt.AddDays(30),
                Status = workspace.Active ? "active" : "expired",
                MaxDrivers = planLimits.MaxDrivers ?? int.MaxValue,
                MaxRoutes = planLimits.IncludedMonthlyStops,
                MaxCustomers = planLimits.MaxCustomers ?? int.MaxValue
            }
        };
    }
}