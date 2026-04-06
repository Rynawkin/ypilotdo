using System.Text.Json.Serialization;
using FluentValidation;
using Google.OrTools.ConstraintSolver;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Data.Journeys;
using Monolith.WebAPI.Hubs;
using Monolith.WebAPI.Infrastructure;
using Monolith.WebAPI.Responses.Journeys;
using Monolith.WebAPI.Services.Members;
using Monolith.WebAPI.Services.Optimization;

namespace Monolith.WebAPI.Applications.Commands.Journeys;

public class ReoptimizeActiveJourneyCommand : BaseAuthenticatedCommand<JourneyResponse>
{
    [JsonIgnore] public override bool RequiresDriver => false;
    [JsonIgnore] public override bool RequiresWorkspaceAccess => false;

    [JsonIgnore] public int JourneyId { get; init; }

    public double CurrentLatitude { get; init; }
    public double CurrentLongitude { get; init; }
    public List<int> DeferredStopIds { get; init; } = new();
}

public class ReoptimizeActiveJourneyCommandValidator : AbstractValidator<ReoptimizeActiveJourneyCommand>
{
    public ReoptimizeActiveJourneyCommandValidator()
    {
        RuleFor(x => x.JourneyId).GreaterThan(0);
        RuleFor(x => x.CurrentLatitude).InclusiveBetween(-90, 90);
        RuleFor(x => x.CurrentLongitude).InclusiveBetween(-180, 180);
        RuleForEach(x => x.DeferredStopIds).GreaterThan(0);
    }
}

public class ReoptimizeActiveJourneyCommandHandler : BaseAuthenticatedCommandHandler<ReoptimizeActiveJourneyCommand, JourneyResponse>
{
    private readonly AppDbContext _context;
    private readonly IRouteMatrixProvider _routeMatrixProvider;
    private readonly IOrderedRouteDetailsProvider _orderedRouteDetailsProvider;
    private readonly IHubContext<JourneyHub> _journeyHub;
    private readonly ILogger<ReoptimizeActiveJourneyCommandHandler> _logger;

    public ReoptimizeActiveJourneyCommandHandler(
        AppDbContext context,
        IUserService userService,
        IRouteMatrixProvider routeMatrixProvider,
        IOrderedRouteDetailsProvider orderedRouteDetailsProvider,
        IHubContext<JourneyHub> journeyHub,
        ILogger<ReoptimizeActiveJourneyCommandHandler> logger) : base(userService)
    {
        _context = context;
        _routeMatrixProvider = routeMatrixProvider;
        _orderedRouteDetailsProvider = orderedRouteDetailsProvider;
        _journeyHub = journeyHub;
        _logger = logger;
    }

    protected override async Task<JourneyResponse> HandleCommand(ReoptimizeActiveJourneyCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("[REOPTIMIZE] Starting reoptimization for journey #{JourneyId}", request.JourneyId);

        var journey = await _context.Journeys
            .Include(j => j.Route)
                .ThenInclude(r => r.Workspace)
            .Include(j => j.Route)
                .ThenInclude(r => r.Stops)
                    .ThenInclude(s => s.Customer)
            .Include(j => j.Stops.OrderBy(s => s.Order))
                .ThenInclude(s => s.RouteStop)
                    .ThenInclude(rs => rs.Customer)
            .Include(j => j.Driver)
            .Include(j => j.Vehicle)
            .Include(j => j.StartDetails)
            .Include(j => j.EndDetails)
            .FirstOrDefaultAsync(x => x.Id == request.JourneyId && x.WorkspaceId == User.WorkspaceId, cancellationToken);

        if (journey is null)
        {
            throw new ApiException("Sefer bulunamadi", 404);
        }

        EnsureDriverCanOnlyAccessOwnJourney(journey);

        if (!journey.IsActive)
        {
            throw new ApiException("Sadece aktif seferlere optimizasyon yapilabilir", 400);
        }

        var pendingStops = journey.Stops
            .Where(s => s.Status == JourneyStopStatus.Pending)
            .OrderBy(s => s.Order)
            .ToList();

        if (pendingStops.Count == 0)
        {
            throw new ApiException("Optimize edilecek durak bulunamadi", 400);
        }

        var depotStop = pendingStops.FirstOrDefault(s => s.RouteStop?.CustomerId == null);
        var customerStops = pendingStops.Where(s => s.Id != depotStop?.Id).ToList();
        var deferredStopIds = request.DeferredStopIds?.Count > 0
            ? request.DeferredStopIds.ToHashSet()
            : new HashSet<int>();

        var firstStops = customerStops
            .Where(s => s.RouteStop?.OrderType == OrderType.First)
            .OrderBy(s => s.Order)
            .ToList();
        var lastStops = customerStops
            .Where(s => s.RouteStop?.OrderType == OrderType.Last)
            .OrderBy(s => s.Order)
            .ToList();
        var deferredStops = customerStops
            .Where(s => s.RouteStop?.OrderType == OrderType.Auto && deferredStopIds.Contains(s.Id))
            .OrderBy(s => s.Order)
            .ToList();
        var autoStops = customerStops
            .Where(s => s.RouteStop?.OrderType == OrderType.Auto && !deferredStopIds.Contains(s.Id))
            .OrderBy(s => s.Order)
            .ToList();

        _logger.LogInformation(
            "[REOPTIMIZE] Pending={PendingCount}, First={FirstCount}, Auto={AutoCount}, Deferred={DeferredCount}, Last={LastCount}, Depot={DepotCount}",
            pendingStops.Count,
            firstStops.Count,
            autoStops.Count,
            deferredStops.Count,
            lastStops.Count,
            depotStop is null ? 0 : 1);

        if (autoStops.Count == 0 && firstStops.Count == 0 && lastStops.Count == 0)
        {
            journey.NeedsReoptimization = false;
            await _context.SaveChangesAsync(cancellationToken);
            return new JourneyResponse(journey);
        }

        var fixedPrefixStops = firstStops;
        var fixedSuffixStops = deferredStops.Concat(lastStops).OrderBy(s => s.Order).ToList();
        var segmentOriginStop = fixedPrefixStops.LastOrDefault();
        var segmentDestinationStop = fixedSuffixStops.FirstOrDefault();

        var optimizedAutoStops = await OptimizeStopsBetweenAnchorsAsync(
            autoStops,
            segmentOriginStop?.EndLatitude ?? request.CurrentLatitude,
            segmentOriginStop?.EndLongitude ?? request.CurrentLongitude,
            segmentDestinationStop?.EndLatitude ?? depotStop?.EndLatitude ?? request.CurrentLatitude,
            segmentDestinationStop?.EndLongitude ?? depotStop?.EndLongitude ?? request.CurrentLongitude,
            cancellationToken);

        var finalOrderedStops = new List<JourneyStop>();
        finalOrderedStops.AddRange(fixedPrefixStops);
        finalOrderedStops.AddRange(optimizedAutoStops);
        finalOrderedStops.AddRange(fixedSuffixStops);
        if (depotStop != null)
        {
            finalOrderedStops.Add(depotStop);
        }

        if (finalOrderedStops.Count == 0)
        {
            journey.NeedsReoptimization = false;
            await _context.SaveChangesAsync(cancellationToken);
            return new JourneyResponse(journey);
        }

        var destinationStop = finalOrderedStops.Last();
        var orderedWaypoints = finalOrderedStops
            .Take(finalOrderedStops.Count - 1)
            .Select(stop => $"{stop.EndLatitude},{stop.EndLongitude}")
            .ToList();
        var finalDestination = $"{destinationStop.EndLatitude},{destinationStop.EndLongitude}";
        var origin = $"{request.CurrentLatitude},{request.CurrentLongitude}";

        var finalRouteDetails = await _orderedRouteDetailsProvider.GetOrderedRouteAsync(
            origin,
            finalDestination,
            orderedWaypoints,
            journey.Route?.AvoidTolls ?? false);

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var turkeyTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time");
            var currentTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, turkeyTimeZone);
            var estimatedDepartureTime = currentTime.TimeOfDay;

            var completedStops = journey.Stops
                .Where(s => s.Status != JourneyStopStatus.Pending)
                .OrderBy(s => s.Order)
                .ToList();
            var newOrder = completedStops.Count > 0 ? completedStops.Max(s => s.Order) + 1 : 1;

            _logger.LogInformation(
                "[REOPTIMIZE] Final stop order: {StopNames}",
                string.Join(" -> ", finalOrderedStops.Select(s => s.RouteStop?.Name ?? "Depot")));

            for (var i = 0; i < finalOrderedStops.Count; i++)
            {
                var stopToUpdate = finalOrderedStops[i];
                stopToUpdate.Order = newOrder++;

                double travelTimeSeconds;
                double distanceMeters;

                if (i < finalRouteDetails.Legs.Count)
                {
                    var leg = finalRouteDetails.Legs[i];
                    travelTimeSeconds = leg.DurationSeconds;
                    distanceMeters = leg.DistanceMeters;
                    _logger.LogInformation(
                        "[REOPTIMIZE] Stop #{Index} using ordered route details ({Provider}): {DurationSeconds}s, {DistanceMeters}m",
                        i,
                        finalRouteDetails.ProviderName,
                        travelTimeSeconds,
                        distanceMeters);
                }
                else
                {
                    var fromLatitude = i == 0 ? request.CurrentLatitude : finalOrderedStops[i - 1].EndLatitude;
                    var fromLongitude = i == 0 ? request.CurrentLongitude : finalOrderedStops[i - 1].EndLongitude;
                    distanceMeters = CalculateDistance(fromLatitude, fromLongitude, stopToUpdate.EndLatitude, stopToUpdate.EndLongitude);
                    travelTimeSeconds = Math.Max(1d, (distanceMeters / 1000d) * 3600d / 40d);
                    _logger.LogWarning("[REOPTIMIZE] Stop #{Index} using Haversine fallback: {DurationSeconds}s", i, travelTimeSeconds);
                }

                var arrivalTimeSpan = estimatedDepartureTime + TimeSpan.FromSeconds(travelTimeSeconds);
                if (arrivalTimeSpan.TotalHours >= 24)
                {
                    arrivalTimeSpan = new TimeSpan(23, 59, 59);
                }

                stopToUpdate.EstimatedArrivalTime = arrivalTimeSpan;
                stopToUpdate.Distance = distanceMeters / 1000d;

                var routeStop = journey.Route.Stops.FirstOrDefault(rs => rs.Id == stopToUpdate.RouteStopId);
                var serviceTime = routeStop?.ServiceTime ?? journey.Route.Workspace.DefaultServiceTime;
                var departureTimeSpan = arrivalTimeSpan + serviceTime;
                if (departureTimeSpan.TotalHours >= 24)
                {
                    departureTimeSpan = new TimeSpan(23, 59, 59);
                }

                stopToUpdate.EstimatedDepartureTime = departureTimeSpan;
                estimatedDepartureTime = departureTimeSpan;
            }

            journey.Polyline = finalRouteDetails.Polyline;
            journey.NeedsReoptimization = false;

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        try
        {
            await _journeyHub.Clients.Group($"journey-{journey.Id}").SendAsync(
                "JourneyReoptimized",
                new
                {
                    JourneyId = journey.Id,
                    Message = "Rota optimize edildi",
                    UpdatedStops = journey.Stops.OrderBy(s => s.Order).Select(s => new
                    {
                        s.Id,
                        s.Order,
                        s.EstimatedArrivalTime,
                        s.EstimatedDepartureTime,
                        CustomerName = s.RouteStop?.Customer?.Name ?? "Bilinmiyor",
                        s.EndAddress
                    })
                },
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REOPTIMIZE] Failed to send SignalR notification");
        }

        journey = await _context.Journeys
            .Include(j => j.Route)
                .ThenInclude(r => r.Stops)
                    .ThenInclude(s => s.Customer)
            .Include(j => j.Stops.OrderBy(s => s.Order))
                .ThenInclude(s => s.RouteStop)
                    .ThenInclude(rs => rs.Customer)
            .Include(j => j.Driver)
            .Include(j => j.Vehicle)
            .Include(j => j.StartDetails)
            .Include(j => j.EndDetails)
            .Include(j => j.Statuses)
            .FirstOrDefaultAsync(x => x.Id == request.JourneyId, cancellationToken);

        return new JourneyResponse(journey!);
    }

    private async Task<List<JourneyStop>> OptimizeStopsBetweenAnchorsAsync(
        List<JourneyStop> stops,
        double originLatitude,
        double originLongitude,
        double destinationLatitude,
        double destinationLongitude,
        CancellationToken cancellationToken)
    {
        if (stops.Count == 0)
        {
            return new List<JourneyStop>();
        }

        if (stops.Count == 1)
        {
            return new List<JourneyStop>(stops);
        }

        var optimizationStops = stops
            .Select(stop => new OptimizationStop
            {
                Id = stop.Id,
                Name = stop.RouteStop?.Name ?? stop.EndAddress ?? $"Stop {stop.Id}",
                Latitude = stop.EndLatitude,
                Longitude = stop.EndLongitude,
                ServiceTimeMinutes = 0,
                OrderType = OrderType.Auto
            })
            .ToList();

        var matrixResult = await _routeMatrixProvider.BuildMatrixAsync(
            originLatitude,
            originLongitude,
            optimizationStops,
            destinationLatitude,
            destinationLongitude);

        var nodeCount = optimizationStops.Count + 2;
        var endNode = nodeCount - 1;
        var manager = new RoutingIndexManager(nodeCount, 1, new[] { 0 }, new[] { endNode });
        var routing = new RoutingModel(manager);

        var transitCallbackIndex = routing.RegisterTransitCallback((fromIndex, toIndex) =>
        {
            var fromNode = manager.IndexToNode(fromIndex);
            var toNode = manager.IndexToNode(toIndex);
            return matrixResult.DistanceMatrix[fromNode, toNode];
        });

        routing.SetArcCostEvaluatorOfAllVehicles(transitCallbackIndex);

        var searchParameters = operations_research_constraint_solver.DefaultRoutingSearchParameters();
        searchParameters.FirstSolutionStrategy = FirstSolutionStrategy.Types.Value.PathCheapestArc;
        searchParameters.LocalSearchMetaheuristic = LocalSearchMetaheuristic.Types.Value.GuidedLocalSearch;
        searchParameters.TimeLimit = new Google.Protobuf.WellKnownTypes.Duration { Seconds = 10 };

        var solution = routing.SolveWithParameters(searchParameters);
        if (solution == null)
        {
            throw new ApiException("Aktif sefer icin yeni rota hesaplanamadi.", 500);
        }

        var orderedStops = new List<JourneyStop>(optimizationStops.Count);
        var addedStopIds = new HashSet<int>();
        var index = routing.Start(0);

        while (!routing.IsEnd(index))
        {
            var node = manager.IndexToNode(index);
            if (node is > 0 && node < endNode)
            {
                var stop = stops[node - 1];
                orderedStops.Add(stop);
                addedStopIds.Add(stop.Id);
            }

            index = solution.Value(routing.NextVar(index));
        }

        foreach (var stop in stops.Where(stop => !addedStopIds.Contains(stop.Id)))
        {
            orderedStops.Add(stop);
        }

        _logger.LogInformation(
            "[REOPTIMIZE] Ordered {StopCount} auto stops using {Provider}",
            orderedStops.Count,
            matrixResult.ProviderName);

        return orderedStops;
    }

    private void EnsureDriverCanOnlyAccessOwnJourney(Journey journey)
    {
        if (!User.IsDriver)
        {
            return;
        }

        if (journey.Driver?.UserId != User.Id)
        {
            throw new ApiException("You can only reoptimize your assigned journey.", 403);
        }
    }

    private static double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        const double earthRadiusMeters = 6371000d;
        var dLat = (lat2 - lat1) * Math.PI / 180d;
        var dLon = (lon2 - lon1) * Math.PI / 180d;
        var a = Math.Sin(dLat / 2d) * Math.Sin(dLat / 2d) +
                Math.Cos(lat1 * Math.PI / 180d) *
                Math.Cos(lat2 * Math.PI / 180d) *
                Math.Sin(dLon / 2d) * Math.Sin(dLon / 2d);
        var c = 2d * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1d - a));
        return earthRadiusMeters * c * 1.4d;
    }
}
