using System.Text.Json.Serialization;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Data.Workspace;
using Monolith.WebAPI.Infrastructure;
using Monolith.WebAPI.Requests;
using Monolith.WebAPI.Responses.Journeys;
using Monolith.WebAPI.Services.Members;
using Route = Monolith.WebAPI.Data.Journeys.Route;

namespace Monolith.WebAPI.Applications.Commands.Journeys;

public class CreateRouteCommand : BaseAuthenticatedCommand<RouteResponse>
{
    [JsonIgnore] public override bool RequiresDispatcher => true;
    [JsonIgnore] public override bool RequiresWorkspaceAccess => true;

    public int DepotId { get; set; }
    public int? DriverId { get; set; }
    public int? VehicleId { get; set; }
    public int? CurrentKm { get; set; } // Current kilometer reading of the vehicle
    public string Name { get; set; }
    public DateTime Date { get; set; }
    public string Notes { get; set; }
    
    public bool Optimized { get; set; }
    public bool AvoidTolls { get; set; } = false;
    public double? TotalDistance { get; set; }
    public int? TotalDuration { get; set; }

    public List<RouteStopRequest> Stops { get; set; }

    public CreateRouteCommandStartDetails StartDetails { get; set; }
    public CreateRouteCommandEndDetails EndDetails { get; set; }

    public Route ToEntity(Depot depot, int workspaceId, TimeSpan defaultServiceTime)
    {
        var route = new Route(this, depot, workspaceId, defaultServiceTime);
        
        if (Optimized)
        {
            route.Optimized = true;
            route.TotalDistance = TotalDistance ?? 0;
            route.TotalDuration = TotalDuration ?? 0;
        }
        
        return route;
    }
}

public class CreateRouteCommandValidator : AbstractValidator<CreateRouteCommand>
{
    public CreateRouteCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.Date).NotEmpty();
        RuleFor(x => x.DepotId).NotEmpty();
        RuleFor(x => x.Notes).MaximumLength(500);
        RuleFor(x => x.TotalDistance).GreaterThanOrEqualTo(0).When(x => x.TotalDistance.HasValue);
        RuleFor(x => x.TotalDuration).GreaterThanOrEqualTo(0).When(x => x.TotalDuration.HasValue);

        // ✅ CurrentKm validation
        RuleFor(x => x.CurrentKm)
            .GreaterThanOrEqualTo(0)
            .When(x => x.CurrentKm.HasValue)
            .WithMessage("Kilometre 0'dan küçük olamaz");

        RuleFor(x => x.StartDetails).SetValidator(new CreateRouteStartDetailsValidator()).When(x => x.StartDetails != null);
        RuleFor(x => x.EndDetails).SetValidator(new CreateRouteEndDetailsValidator()).When(x => x.EndDetails != null);
        RuleFor(x => x.Stops).NotEmpty().WithMessage("Route must have at least one stop");
        RuleForEach(x => x.Stops).SetValidator(new StopRequestModelValidator());
    }
}

public class CreateRouteCommandHandler(AppDbContext context, IUserService userService)
    : BaseAuthenticatedCommandHandler<CreateRouteCommand, RouteResponse>(userService)
{
    protected override async Task<RouteResponse> HandleCommand(CreateRouteCommand request, CancellationToken cancellationToken)
    {
        var depot = await context.Depots
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.DepotId && x.WorkspaceId == User.WorkspaceId, cancellationToken);
        if (depot == null)
            throw new ApiException("Depot not found.", 404);

        var route = request.ToEntity(depot, User.WorkspaceId, User.Workspace.DefaultServiceTime);

        // DEBUG: Time window override'ları kontrol et
        Console.WriteLine("=== CREATE ROUTE - TIME WINDOW DEBUG ===");
        foreach (var requestStop in request.Stops)
        {
            Console.WriteLine($"Request Stop: {requestStop.Name} - ArriveBetweenStart: {requestStop.ArriveBetweenStart}, ArriveBetweenEnd: {requestStop.ArriveBetweenEnd}");
        }
        foreach (var routeStop in route.Stops)
        {
            Console.WriteLine($"Route Stop: {routeStop.Name} - ArriveBetweenStart: {routeStop.ArriveBetweenStart}, ArriveBetweenEnd: {routeStop.ArriveBetweenEnd}");
        }

        // Customer bilgilerini include et
        foreach (var stop in route.Stops)
        {
            if (stop.CustomerId.HasValue)
            {
                stop.Customer = await context.Customers
                    .FirstOrDefaultAsync(x => x.Id == stop.CustomerId.Value && x.WorkspaceId == User.WorkspaceId, cancellationToken);
                
                if (stop.Customer == null)
                {
                    throw new ApiException($"Customer with ID {stop.CustomerId} not found.", 404);
                }
            }
        }
        
        // Driver ve Vehicle'ı include et
        if (route.DriverId.HasValue)
        {
            route.Driver = await context.Drivers
                .FirstOrDefaultAsync(x => x.Id == route.DriverId.Value && x.WorkspaceId == User.WorkspaceId, cancellationToken);
        }
        
        if (route.VehicleId.HasValue)
        {
            route.Vehicle = await context.Vehicles
                .FirstOrDefaultAsync(x => x.Id == route.VehicleId.Value && x.WorkspaceId == User.WorkspaceId, cancellationToken);

            // Update vehicle's current kilometer if provided
            if (route.Vehicle != null && request.CurrentKm.HasValue)
            {
                route.Vehicle.CurrentKm = request.CurrentKm.Value;
                context.Vehicles.Update(route.Vehicle);
            }
        }

        await context.Routes.AddAsync(route, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        route.Depot = depot;

        return new RouteResponse(route);
    }
}