using System.Text.Json.Serialization;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Data.Journeys;
using Monolith.WebAPI.External.Google;
using Monolith.WebAPI.External.RouteXL;
using Monolith.WebAPI.Infrastructure;
using Monolith.WebAPI.Responses.Journeys;
using Monolith.WebAPI.Services.Members;

namespace Monolith.WebAPI.Applications.Commands.Journeys;

public class OptimizeJourneyCommand : BaseAuthenticatedCommand<JourneyResponse>
{
    [JsonIgnore] public override bool RequiresDispatcher => true; // Dispatcher ve üstü yapabilir
    [JsonIgnore] public override bool RequiresWorkspaceAccess => true;
    [JsonIgnore] public int JourneyId { get; set; }
}

public class OptimizeJourneyCommandValidator : AbstractValidator<OptimizeJourneyCommand>
{
    public OptimizeJourneyCommandValidator()
    {
        RuleFor(x => x.JourneyId).NotEmpty();
    }
}

public class OptimizeJourneyCommandHandler(
    AppDbContext context,
    GoogleApiService googleApiService,
    RouteXlService routeXlService,
    IUserService userService)
    : BaseAuthenticatedCommandHandler<OptimizeJourneyCommand, JourneyResponse>(userService)
{
    override protected async Task<JourneyResponse> HandleCommand(OptimizeJourneyCommand request, CancellationToken cancellationToken)
    {
        var journey = await context.Journeys
            .Include(x => x.StartDetails)
            .Include(x => x.EndDetails)
            .Include(x => x.Route)
                .ThenInclude(x => x.Workspace)
            .Include(x => x.Route)
                .ThenInclude(x => x.Stops)
                    .ThenInclude(s => s.Customer)
            .Include(x => x.Driver)
            .Include(x => x.Stops)
            .AsSplitQuery()
            .FirstOrDefaultAsync(x => x.Id == request.JourneyId && x.Route.WorkspaceId == User.WorkspaceId, cancellationToken);

        if (journey is null)
            throw new ApiException("Journey not found", 404);

        if (journey.Route?.Stops is null || journey.Route?.Stops?.Count == 0)
            throw new ApiException("Route stops not found", 404);

        await Optimize(journey, cancellationToken);

        // Journey stops'larını ekle
        foreach (var s in journey.Stops)
            context.Entry(s).State = EntityState.Added;

        // Route'u güncelle
        if (journey.Route != null)
        {
            context.Routes.Update(journey.Route);
        }

        context.Journeys.Update(journey);
        await context.SaveChangesAsync(cancellationToken);

        // Journey'i tüm ilişkili verilerle tekrar yükle
        journey = await context.Journeys
            .Include(x => x.Route)
                .ThenInclude(r => r.Stops)
                    .ThenInclude(s => s.Customer)
            .Include(x => x.Route)
                .ThenInclude(r => r.Driver)
            .Include(x => x.Route)
                .ThenInclude(r => r.Vehicle)
            .Include(x => x.Route)
                .ThenInclude(r => r.Depot)
            .Include(x => x.Route)
                .ThenInclude(r => r.StartDetails)
            .Include(x => x.Route)
                .ThenInclude(r => r.EndDetails)
            .Include(x => x.Stops)
                .ThenInclude(s => s.RouteStop)
                    .ThenInclude(rs => rs.Customer)
            .Include(x => x.Driver)
            .Include(x => x.StartDetails)
            .Include(x => x.EndDetails)
            .Include(x => x.Statuses)
            .FirstOrDefaultAsync(x => x.Id == request.JourneyId, cancellationToken);

        return new JourneyResponse(journey);
    }

    private async Task Optimize(Journey journey, CancellationToken cancellationToken)
    {
        try
        {
            // Önce RouteXL'i dene
            await RouteXlOptimizeAsync(journey, cancellationToken);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"RouteXL optimization failed: {ex.Message}, trying Google...");
            // RouteXL başarısız olursa Google'ı dene
            await GoogleOptimizeAsync(journey, cancellationToken);
        }
    }

    private async Task GoogleOptimizeAsync(Journey journey, CancellationToken cancellationToken)
    {
        if (journey.Route.Stops.Any(x => x.ArriveBetweenStart.HasValue))
            throw new ApiException(
                "Optimizing service is not available with time windows. Please try again later or remove time windows.", 503, 1000);

        var (origin, destination, waypoints) = journey.GetGoogleParams();
        var waypointIds = waypoints.Keys.ToList();

        // Google Directions API kullanarak optimize et
        var waypointsList = waypoints.Values.ToList();
        var googleResponse = await googleApiService.GetDirections(
            origin, 
            destination, 
            waypointsList, 
            true  // optimize: true
        );

        if (googleResponse == null)
            throw new ApiException("Google Directions API returned no response", 500);

        // Journey stops'larını Google response'tan oluştur - YENİ MODEL İLE
        journey.CreateStops(googleResponse, waypointIds.AsReadOnly());
        
        // Route metriklerini güncelle
        if (googleResponse.Routes?.FirstOrDefault() != null)
        {
            var route = googleResponse.Routes.First();
            double totalDistance = 0;
            int totalDuration = 0;
            
            if (route.Legs != null)
            {
                foreach (var leg in route.Legs)
                {
                    if (leg.Distance != null && leg.Duration != null)
                    {
                        totalDistance += leg.Distance.Value / 1000.0; // metre'den km'ye
                        totalDuration += (int)(leg.Duration.Value / 60); // saniyeden dakikaya
                    }
                }
            }
            
            journey.Route.TotalDistance = totalDistance;
            journey.Route.TotalDuration = totalDuration;
            journey.Route.Optimized = true;
        }
    }

    private async Task RouteXlOptimizeAsync(Journey journey, CancellationToken cancellationToken)
    {
        var routeXlOptimizingResponse = await routeXlService.OptimizeAsync(journey.GetRouteXlParams(), cancellationToken);

        if (routeXlOptimizingResponse == null || routeXlOptimizingResponse.Route == null)
            throw new ApiException("RouteXL optimization failed", 500);

        var (origin, destination, _) = journey.GetGoogleParams();
        
        // RouteXL'den gelen optimize edilmiş sırayı al
        var waypoints = (from routeXl in routeXlOptimizingResponse.Route.Skip(1).Take(routeXlOptimizingResponse.Route.Count - 2)
            select journey.Route.Stops.FirstOrDefault(x => x.Id == int.Parse(routeXl.Value.Name))
            into routeStop
            where routeStop is not null
            select routeStop.LatLng).ToList();

        // Google Directions API'den detaylı bilgileri al
        var googleResponse = await googleApiService.GetDirections(origin, destination, waypoints, false);
        
        if (googleResponse?.Routes?.FirstOrDefault() != null)
        {
            var route = googleResponse.Routes.First();
            double totalDistance = 0;
            int totalDuration = 0;
            
            if (route.Legs != null)
            {
                foreach (var leg in route.Legs)
                {
                    if (leg.Distance != null && leg.Duration != null)
                    {
                        totalDistance += leg.Distance.Value / 1000.0;
                        totalDuration += (int)(leg.Duration.Value / 60);
                    }
                }
            }
            
            journey.Route.TotalDistance = totalDistance;
            journey.Route.TotalDuration = totalDuration;
            journey.Route.Optimized = true;
        }
        
        var polyline = googleResponse?.Routes?.FirstOrDefault()?.OverviewPolyline?.Points;
        
        // Journey stops'larını RouteXL response'tan oluştur
        journey.CreateStops(routeXlOptimizingResponse, polyline);
    }
}