// src/Monolith.WebAPI/Applications/Commands/Admin/UpdateWorkspaceStatusCommand.cs
using MediatR;
using Microsoft.EntityFrameworkCore;
using Monolith.WebAPI.Applications.Commands;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Infrastructure;
using Monolith.WebAPI.Responses.Admin;
using Monolith.WebAPI.Services.Members;

namespace Monolith.WebAPI.Applications.Commands.Admin;

public class UpdateWorkspaceStatusCommand : BaseAuthenticatedCommand<WorkspaceDetailResponse>
{
    public int WorkspaceId { get; set; }
    public bool Active { get; set; }
    
    public override bool RequiresSuperAdmin => true;
}

public class UpdateWorkspaceStatusCommandHandler(IUserService userService, AppDbContext context) 
    : BaseAuthenticatedCommandHandler<UpdateWorkspaceStatusCommand, WorkspaceDetailResponse>(userService)
{
    private readonly AppDbContext _context = context;

    protected override async Task<WorkspaceDetailResponse> HandleCommand(UpdateWorkspaceStatusCommand request, CancellationToken cancellationToken)
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
            throw new ApiException($"Workspace with ID {request.WorkspaceId} not found", 404);

        // Workspace status'unu güncelle (reflection ile private setter'a erişim)
        var activeProperty = typeof(Data.Workspace.Workspace).GetProperty(nameof(Data.Workspace.Workspace.Active));
        activeProperty?.SetValue(workspace, request.Active);

        workspace.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        var lastActivity = workspace.UpdatedAt;

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
            Active = request.Active,
            CreatedAt = workspace.CreatedAt,
            UserCount = workspace.Users.Count,
            DriverCount = workspace.Drivers.Count,
            VehicleCount = workspace.Vehicles.Count,
            CustomerCount = workspace.Customers.Count,
            RouteCount = workspace.Routes.Count,
            DepotCount = workspace.Depots.Count,
            LastActivity = lastActivity,
            Subscription = new SubscriptionResponse
            {
                Plan = GetPlanType(workspace.CreatedAt),
                StartDate = workspace.CreatedAt,
                EndDate = workspace.CreatedAt.AddDays(30),
                Status = request.Active ? "active" : "expired",
                MaxDrivers = workspace.MaximumDriverCount,
                MaxRoutes = 100,
                MaxCustomers = 1000
            }
        };
    }

    private static string GetPlanType(DateTime createdAt)
    {
        var daysOld = (DateTime.UtcNow - createdAt).Days;
        return daysOld <= 30 ? "trial" : "basic";
    }
}