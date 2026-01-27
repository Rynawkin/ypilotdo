using System.Text.Json.Serialization;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Data.Journeys;
using Monolith.WebAPI.Infrastructure;
using Monolith.WebAPI.Requests;
using Monolith.WebAPI.Responses.Journeys;
using Monolith.WebAPI.Services.Members;

namespace Monolith.WebAPI.Applications.Commands.Journeys;

public class UpdateRouteCommand : BaseAuthenticatedCommand<RouteResponse>
{
    [JsonIgnore] public override bool RequiresDispatcher => true;
    [JsonIgnore] public override bool RequiresWorkspaceAccess => true;
    [JsonIgnore] public int RouteId { get; set; }

    public string? Name { get; set; }
    public DateTime? Date { get; set; }
    public int? DepotId { get; set; }
    public int? DriverId { get; set; }
    public int? VehicleId { get; set; }
    public int? CurrentKm { get; set; } // ✅ YENİ - Araç kilometresi güncelleme
    public List<RouteStopRequest>? Stops { get; set; }
    public CreateRouteCommandStartDetails? StartDetails { get; set; }
    public CreateRouteCommandEndDetails? EndDetails { get; set; }
    public bool? Optimized { get; set; }
    public double? TotalDistance { get; set; }
    public int? TotalDuration { get; set; }
}

public class UpdateRouteCommandValidator : AbstractValidator<UpdateRouteCommand>
{
    public UpdateRouteCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().When(x => x.Name != null);
        RuleFor(x => x.Date).NotEmpty().When(x => x.Date.HasValue);
        RuleFor(x => x.DepotId).GreaterThan(0).When(x => x.DepotId.HasValue);

        // ✅ CurrentKm validation
        RuleFor(x => x.CurrentKm)
            .GreaterThanOrEqualTo(0)
            .When(x => x.CurrentKm.HasValue)
            .WithMessage("Kilometre 0'dan küçük olamaz");
    }
}

public class UpdateRouteCommandHandler(AppDbContext context, IUserService userService)
    : BaseAuthenticatedCommandHandler<UpdateRouteCommand, RouteResponse>(userService)
{
    protected override async Task<RouteResponse> HandleCommand(UpdateRouteCommand request, CancellationToken cancellationToken)
    {
        var route = await context.Routes
            .Include(x => x.Depot)
            .Include(x => x.Stops)
            .Include(x => x.StartDetails)
            .Include(x => x.EndDetails)
            .FirstOrDefaultAsync(x => x.Id == request.RouteId && x.WorkspaceId == User.WorkspaceId, cancellationToken);

        if (route == null)
            throw new ApiException("Route not found.", 404);

        // Update basic fields
        if (!string.IsNullOrEmpty(request.Name))
        {
            var nameProperty = typeof(Monolith.WebAPI.Data.Journeys.Route).GetProperty(nameof(Monolith.WebAPI.Data.Journeys.Route.Name));
            nameProperty?.SetValue(route, request.Name);
            route.Status = "planned";
        }

        if (request.Date.HasValue)
        {
            var dateProperty = typeof(Monolith.WebAPI.Data.Journeys.Route).GetProperty(nameof(Monolith.WebAPI.Data.Journeys.Route.Date));
            dateProperty?.SetValue(route, request.Date.Value);
        }

        // Update depot if provided
        if (request.DepotId.HasValue)
        {
            var depot = await context.Set<Data.Workspace.Depot>()
                .FirstOrDefaultAsync(d => d.Id == request.DepotId.Value && d.WorkspaceId == User.WorkspaceId, cancellationToken);
            if (depot == null)
                throw new ApiException("Depot not found.", 404);
            
            var depotIdProperty = typeof(Monolith.WebAPI.Data.Journeys.Route).GetProperty(nameof(Monolith.WebAPI.Data.Journeys.Route.DepotId));
            depotIdProperty?.SetValue(route, request.DepotId.Value);
        }

        // Update driver if provided
        if (request.DriverId.HasValue)
        {
            var driver = await context.Set<Data.Workspace.Driver>()
                .FirstOrDefaultAsync(d => d.Id == request.DriverId.Value && d.WorkspaceId == User.WorkspaceId, cancellationToken);
            if (driver == null)
                throw new ApiException("Driver not found.", 404);
            route.DriverId = request.DriverId.Value;
        }

        // Update vehicle if provided
        if (request.VehicleId.HasValue)
        {
            var vehicle = await context.Set<Data.Workspace.Vehicle>()
                .FirstOrDefaultAsync(v => v.Id == request.VehicleId.Value && v.WorkspaceId == User.WorkspaceId, cancellationToken);
            if (vehicle == null)
                throw new ApiException("Vehicle not found.", 404);
            route.VehicleId = request.VehicleId.Value;

            // ✅ YENİ: CurrentKm güncellemesi
            if (request.CurrentKm.HasValue)
            {
                vehicle.CurrentKm = request.CurrentKm.Value;
                context.Vehicles.Update(vehicle);
            }
        }
        // ✅ YENİ: VehicleId değişmese bile CurrentKm güncellenebilir
        else if (request.CurrentKm.HasValue && route.VehicleId.HasValue)
        {
            var vehicle = await context.Set<Data.Workspace.Vehicle>()
                .FirstOrDefaultAsync(v => v.Id == route.VehicleId.Value && v.WorkspaceId == User.WorkspaceId, cancellationToken);
            if (vehicle != null)
            {
                vehicle.CurrentKm = request.CurrentKm.Value;
                context.Vehicles.Update(vehicle);
            }
        }

        // Update StartDetails
        if (request.StartDetails != null)
        {
            if (route.StartDetails == null)
            {
                route.StartDetails = new RouteStartDetails
                {
                    RouteId = route.Id,
                    StartTime = request.StartDetails.StartTime,
                    Name = request.StartDetails.Name ?? "",
                    Address = request.StartDetails.Address ?? "",
                    Latitude = request.StartDetails.Latitude,
                    Longitude = request.StartDetails.Longitude
                };
                context.Add(route.StartDetails);
            }
            else
            {
                route.StartDetails.StartTime = request.StartDetails.StartTime;
                if (!string.IsNullOrEmpty(request.StartDetails.Name))
                    route.StartDetails.Name = request.StartDetails.Name;
                if (!string.IsNullOrEmpty(request.StartDetails.Address))
                    route.StartDetails.Address = request.StartDetails.Address;
                route.StartDetails.Latitude = request.StartDetails.Latitude;
                route.StartDetails.Longitude = request.StartDetails.Longitude;
            }
        }

        // Update EndDetails
        if (request.EndDetails != null)
        {
            if (route.EndDetails == null)
            {
                route.EndDetails = new RouteEndDetails
                {
                    RouteId = route.Id,
                    Name = request.EndDetails.Name ?? "",
                    Address = request.EndDetails.Address ?? "",
                    Latitude = request.EndDetails.Latitude,
                    Longitude = request.EndDetails.Longitude
                };
                context.Add(route.EndDetails);
            }
            else
            {
                if (!string.IsNullOrEmpty(request.EndDetails.Name))
                    route.EndDetails.Name = request.EndDetails.Name;
                if (!string.IsNullOrEmpty(request.EndDetails.Address))
                    route.EndDetails.Address = request.EndDetails.Address;
                route.EndDetails.Latitude = request.EndDetails.Latitude;
                route.EndDetails.Longitude = request.EndDetails.Longitude;
            }
        }

        // Update stops if provided
        if (request.Stops != null && request.Stops.Any())
        {
            // Remove existing stops
            context.RemoveRange(route.Stops);

            // Add new stops preserving ETA values
            var order = 1;
            foreach (var stop in request.Stops)
            {
                var routeStop = new RouteStop(stop, route.Id, User.Workspace.DefaultServiceTime);
                routeStop.Order = order++;

                // Preserve ETA values from frontend
                routeStop.EstimatedArrivalTime = stop.EstimatedArrivalTime;
                routeStop.EstimatedDepartureTime = stop.EstimatedDepartureTime;

                context.Add(routeStop);
            }
        }

        // Update metrics
        route.TotalDeliveries = request.Stops?.Count ?? route.Stops?.Count ?? 0;

        // Update optimization metrics if provided
        if (request.Optimized.HasValue)
            route.Optimized = request.Optimized.Value;
        if (request.TotalDistance.HasValue)
            route.TotalDistance = request.TotalDistance.Value;
        if (request.TotalDuration.HasValue)
            route.TotalDuration = request.TotalDuration.Value;

        await context.SaveChangesAsync(cancellationToken);
        
        // Reload with includes for response
        route = await context.Routes
            .Include(x => x.Depot)
            .Include(x => x.Driver)
            .Include(x => x.Vehicle)
            .Include(x => x.Stops)
            .Include(x => x.StartDetails)
            .Include(x => x.EndDetails)
            .FirstOrDefaultAsync(x => x.Id == request.RouteId, cancellationToken);

        return new RouteResponse(route);
    }
}