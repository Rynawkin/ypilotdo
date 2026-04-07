// src/Monolith.WebAPI/Applications/Commands/Admin/UpdateWorkspacePlanCommand.cs
using MediatR;
using Microsoft.EntityFrameworkCore;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Data.Workspace;
using Monolith.WebAPI.Infrastructure;
using Monolith.WebAPI.Responses.Admin;
using Monolith.WebAPI.Services.Members;
using Monolith.WebAPI.Services.Subscription;

namespace Monolith.WebAPI.Applications.Commands.Admin;

public class UpdateWorkspacePlanCommand : BaseAuthenticatedCommand<WorkspaceDetailResponse>
{
    public int WorkspaceId { get; set; }
    public PlanType NewPlanType { get; set; }
    
    public override bool RequiresSuperAdmin => true;
}

public class UpdateWorkspacePlanCommandHandler : BaseAuthenticatedCommandHandler<UpdateWorkspacePlanCommand, WorkspaceDetailResponse>
{
    private readonly AppDbContext _context;
    private readonly ISubscriptionService _subscriptionService;

    public UpdateWorkspacePlanCommandHandler(IUserService userService, AppDbContext context, ISubscriptionService subscriptionService)
        : base(userService)
    {
        _context = context;
        _subscriptionService = subscriptionService;
    }

    protected override async Task<WorkspaceDetailResponse> HandleCommand(UpdateWorkspacePlanCommand request, CancellationToken cancellationToken)
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

        // Plan güncelle
        workspace.UpdatePlan(request.NewPlanType);
        workspace.SyncLegacyDriverLimit(_subscriptionService.GetPlanLimits(request.NewPlanType).MaxDrivers);
        workspace.SetActive(true);

        await _context.SaveChangesAsync(cancellationToken);

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
            PlanType = workspace.PlanType,
            PlanStartDate = workspace.PlanStartDate,
            CurrentMonthStops = workspace.CurrentMonthStops,
            CurrentMonthAdditionalCharges = workspace.CurrentMonthAdditionalCharges,
            CurrentMonthWhatsAppMessages = workspace.CurrentMonthWhatsAppMessages
        };
    }
}
