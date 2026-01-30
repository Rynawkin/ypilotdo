using System.Text.Json.Serialization;
using FluentValidation;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Data.Journeys;
using Monolith.WebAPI.External.Google;
using Monolith.WebAPI.Hubs;
using Monolith.WebAPI.Infrastructure;
using Monolith.WebAPI.Responses.Journeys;
using Monolith.WebAPI.Services.Members;

namespace Monolith.WebAPI.Applications.Commands.Journeys;

public class ReoptimizeActiveJourneyCommand : BaseAuthenticatedCommand<JourneyResponse>
{
    [JsonIgnore] public override bool RequiresDriver => false; // Şoför de çağırabilir
    [JsonIgnore] public override bool RequiresWorkspaceAccess => true;

    [JsonIgnore] public int JourneyId { get; init; }

    // Şoförün anlık konumu
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
    private readonly GoogleApiService _googleApiService;
    private readonly IHubContext<JourneyHub> _journeyHub;
    private readonly ILogger<ReoptimizeActiveJourneyCommandHandler> _logger;

    public ReoptimizeActiveJourneyCommandHandler(
        AppDbContext context,
        IUserService userService,
        GoogleApiService googleApiService,
        IHubContext<JourneyHub> journeyHub,
        ILogger<ReoptimizeActiveJourneyCommandHandler> logger) : base(userService)
    {
        _context = context;
        _googleApiService = googleApiService;
        _journeyHub = journeyHub;
        _logger = logger;
    }

    protected override async Task<JourneyResponse> HandleCommand(ReoptimizeActiveJourneyCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation($"[REOPTIMIZE] Starting reoptimization for journey #{request.JourneyId}");

        // Journey'i yükle
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
            throw new ApiException("Sefer bulunamadı", 404);

        // Sadece aktif seferlere optimizasyon yapılabilir
        if (!journey.IsActive)
            throw new ApiException("Sadece aktif seferlere optimizasyon yapılabilir", 400);

        // Pending (tamamlanmamış) durakları al
        var pendingStops = journey.Stops
            .Where(s => s.Status == JourneyStopStatus.Pending)
            .OrderBy(s => s.Order)
            .ToList();

        if (pendingStops.Count == 0)
        {
            _logger.LogWarning($"[REOPTIMIZE] No pending stops found for journey #{request.JourneyId}");
            throw new ApiException("Optimize edilecek durak bulunamadı", 400);
        }

        _logger.LogInformation($"[REOPTIMIZE] Found {pendingStops.Count} pending stops");

        // ✅ YENİ: Position constraints'e göre ayır
        var firstStops = pendingStops.Where(s => s.RouteStop != null && s.RouteStop.OrderType == OrderType.First).ToList();
        var lastStops = pendingStops.Where(s => s.RouteStop != null && s.RouteStop.OrderType == OrderType.Last).ToList();
        var autoStops = pendingStops.Where(s => s.RouteStop != null && s.RouteStop.OrderType == OrderType.Auto).ToList();

        // Depot stop (CustomerId == null) - always last
        var depotStop = pendingStops.FirstOrDefault(s => s.RouteStop?.CustomerId == null);

        if (depotStop != null)
        {
            firstStops = firstStops.Where(s => s.Id != depotStop.Id).ToList();
            lastStops = lastStops.Where(s => s.Id != depotStop.Id).ToList();
            autoStops = autoStops.Where(s => s.Id != depotStop.Id).ToList();
        }

        var deferredStopIdSet = request.DeferredStopIds != null && request.DeferredStopIds.Count > 0
            ? request.DeferredStopIds.ToHashSet()
            : new HashSet<int>();
        var deferredStops = autoStops.Where(s => deferredStopIdSet.Contains(s.Id)).ToList();
        var stopsToOptimize = autoStops.Where(s => !deferredStopIdSet.Contains(s.Id)).ToList();

        _logger.LogInformation($"[REOPTIMIZE] Position constraints - First: {firstStops.Count}, Last: {lastStops.Count}, Auto: {autoStops.Count}, Deferred: {deferredStops.Count}, Depot: {(depotStop != null ? 1 : 0)}");

        if (autoStops.Count == 0 && firstStops.Count == 0 && lastStops.Count == 0)
        {
            _logger.LogInformation($"[REOPTIMIZE] Only depot stop remaining, no optimization needed");
            journey.NeedsReoptimization = false;
            await _context.SaveChangesAsync(cancellationToken);
            return new JourneyResponse(journey);
        }

        _logger.LogInformation($"[REOPTIMIZE] Optimizing {stopsToOptimize.Count} auto stops, keeping first ({firstStops.Count}) and last ({lastStops.Count}) stops fixed, deferring {deferredStops.Count} auto stops");

        // Google Maps Directions API parametrelerini hazırla
        var origin = $"{request.CurrentLatitude},{request.CurrentLongitude}"; // Şoförün konumu

        // ✅ YENİ: Destination depot olmalı (eğer varsa)
        var destination = depotStop != null
            ? $"{depotStop.EndLatitude},{depotStop.EndLongitude}"
            : $"{request.CurrentLatitude},{request.CurrentLongitude}"; // Depot yoksa mevcut konum

        try
        {
        // Ara duraklar (waypoints)
        var optimizedAutoStops = new List<JourneyStop>();

        if (stopsToOptimize.Count > 0)
        {
            var waypoints = new Dictionary<int, string>();
            var waypointStopIds = new List<int>();

            foreach (var stop in stopsToOptimize)
            {
                waypoints.Add(stop.Id, $"{stop.EndLatitude},{stop.EndLongitude}");
                waypointStopIds.Add(stop.Id);
            }

            _logger.LogInformation($"[REOPTIMIZE] Origin: Driver location ({origin}), Destination: Depot ({destination}), Waypoints: {waypoints.Count}");

            var waypointsList = waypoints.Values.ToList();
            var directionsResponse = await _googleApiService.GetDirections(
                origin,
                destination,
                waypointsList,
                optimize: true,
                avoidTolls: journey.Route?.AvoidTolls ?? false
            );

            if (directionsResponse?.Routes == null || directionsResponse.Routes.Count == 0)
            {
                throw new ApiException("Google Maps'ten rota alinamadi", 500);
            }

            var route = directionsResponse.Routes.First();
            var waypointOrder = route.WaypointOrder ?? directionsResponse.WaypointOrder ?? new List<int>();

            if (waypointOrder.Count == 0)
            {
                optimizedAutoStops.AddRange(stopsToOptimize);
            }
            else
            {
                foreach (var optimizedIndex in waypointOrder)
                {
                    if (optimizedIndex < waypointStopIds.Count)
                    {
                        var stopId = waypointStopIds[optimizedIndex];
                        var stop = stopsToOptimize.First(s => s.Id == stopId);
                        optimizedAutoStops.Add(stop);
                    }
                }
            }
        }

        var finalOrderedStops = new List<JourneyStop>();
        finalOrderedStops.AddRange(firstStops);
        finalOrderedStops.AddRange(optimizedAutoStops);
        finalOrderedStops.AddRange(deferredStops);
        finalOrderedStops.AddRange(lastStops);
        if (depotStop != null)
        {
            finalOrderedStops.Add(depotStop);
        }

        if (finalOrderedStops.Count == 0)
        {
            _logger.LogInformation($"[REOPTIMIZE] No stops left after ordering, skipping reoptimization");
            journey.NeedsReoptimization = false;
            await _context.SaveChangesAsync(cancellationToken);
            return new JourneyResponse(journey);
        }

        var destinationStop = finalOrderedStops.Last();
        var finalWaypoints = finalOrderedStops
            .Take(Math.Max(0, finalOrderedStops.Count - 1))
            .Select(stop => $"{stop.EndLatitude},{stop.EndLongitude}")
            .ToList();
        var finalDestination = $"{destinationStop.EndLatitude},{destinationStop.EndLongitude}";

        _logger.LogInformation($"[REOPTIMIZE] Final order count: {finalOrderedStops.Count}, Waypoints: {finalWaypoints.Count}");

        var finalDirectionsResponse = await _googleApiService.GetDirections(
            origin,
            finalDestination,
            finalWaypoints,
            optimize: false,
            avoidTolls: journey.Route?.AvoidTolls ?? false
        );

        if (finalDirectionsResponse?.Routes == null || finalDirectionsResponse.Routes.Count == 0)
        {
            throw new ApiException("Google Maps'ten rota alinamadi", 500);
        }

        var finalRoute = finalDirectionsResponse.Routes.First();
        var legs = finalRoute.Legs;

        // BUGFIX S3.15: Use transaction to ensure atomic updates (all or nothing)
        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
                // Optimize edilmiş sıra ile durakları yeniden düzenle
                // ✅ Türkiye saatini al (server UTC'de olabilir)
                var turkeyTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time");
                var currentTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, turkeyTimeZone);

                // ✅ HER ZAMAN mevcut saatten başla (telefonun mevcut saati)
                TimeSpan estimatedDepartureTime = currentTime.TimeOfDay;
                _logger.LogInformation($"[REOPTIMIZE] Starting ETA calculation from Turkey time: {estimatedDepartureTime} (UTC: {DateTime.UtcNow.TimeOfDay})");

                // Tamamlanmış durakları koru
                var completedStops = journey.Stops
                    .Where(s => s.Status != JourneyStopStatus.Pending)
                    .OrderBy(s => s.Order)
                    .ToList();

                int newOrder = completedStops.Count > 0 ? completedStops.Max(s => s.Order) + 1 : 1;

                // ✅ YENİ: Final stop order'ı oluştur: First → Optimized Auto → Last → Depot
                _logger.LogInformation($"[REOPTIMIZE] Final stop order: {string.Join(" → ", finalOrderedStops.Select(s => s.RouteStop?.Name ?? "Depot"))}");

                // ✅ OPTIMIZED: ETA hesaplama - Google Directions legs array'ini kullan
                for (int i = 0; i < finalOrderedStops.Count; i++)
                {
                    var stopToUpdate = finalOrderedStops[i];

                    // Order'ı güncelle
                    stopToUpdate.Order = newOrder++;

                    // Önceki konumdan bu durağa travel time hesapla
                    double travelTimeSeconds = 0;
                    double distanceMeters = 0;

                    // ⭐ Google Directions legs array'inden gerçek süreleri al
                    if (legs != null && i < legs.Count)
                    {
                        var leg = legs[i];
                        travelTimeSeconds = leg.Duration?.Value ?? 0;
                        distanceMeters = leg.Distance?.Value ?? 0;
                        _logger.LogInformation($"[REOPTIMIZE] Stop #{i} using Google Directions data: {travelTimeSeconds}s, {distanceMeters}m");
                    }
                    else
                    {
                        // ❌ FALLBACK: Legs yoksa Haversine kullan
                        if (i == 0)
                        {
                            // İlk durak - şoförün konumundan
                            var distance = CalculateDistance(
                                request.CurrentLatitude, request.CurrentLongitude,
                                stopToUpdate.EndLatitude, stopToUpdate.EndLongitude);
                            travelTimeSeconds = (distance / 1000.0) * 3600 / 40;
                            distanceMeters = distance;
                        }
                        else
                        {
                            // Sonraki duraklar - önceki duraktan
                            var prevStop = finalOrderedStops[i - 1];
                            var distance = CalculateDistance(
                                prevStop.EndLatitude, prevStop.EndLongitude,
                                stopToUpdate.EndLatitude, stopToUpdate.EndLongitude);
                            travelTimeSeconds = (distance / 1000.0) * 3600 / 40;
                            distanceMeters = distance;
                        }
                        _logger.LogWarning($"[REOPTIMIZE] Stop #{i} using Haversine fallback: {travelTimeSeconds}s");
                    }

                    // ETA hesapla
                    var arrivalTimeSpan = estimatedDepartureTime + TimeSpan.FromSeconds(travelTimeSeconds);

                    // ✅ SQL Server Time tipi maksimum 23:59:59 değerini alabilir
                    if (arrivalTimeSpan.TotalHours >= 24)
                    {
                        _logger.LogWarning($"[REOPTIMIZE] Stop #{stopToUpdate.Id} arrival time exceeded 24 hours ({arrivalTimeSpan.TotalHours:F1}h). Capping at 23:59:59.");
                        arrivalTimeSpan = new TimeSpan(23, 59, 59);
                    }

                    stopToUpdate.EstimatedArrivalTime = arrivalTimeSpan;

                    // ⭐ Gerçek mesafeyi kaydet (Google'dan gelen)
                    stopToUpdate.Distance = distanceMeters / 1000.0; // metre → km

                    // Service time ekle
                    var routeStop = journey.Route.Stops.FirstOrDefault(rs => rs.Id == stopToUpdate.RouteStopId);
                    var serviceTime = routeStop?.ServiceTime ?? journey.Route.Workspace.DefaultServiceTime;
                    var departureTimeSpan = arrivalTimeSpan + serviceTime;

                    // ✅ Departure time kontrolü
                    if (departureTimeSpan.TotalHours >= 24)
                    {
                        _logger.LogWarning($"[REOPTIMIZE] Stop #{stopToUpdate.Id} departure time exceeded 24 hours. Capping at 23:59:59.");
                        departureTimeSpan = new TimeSpan(23, 59, 59);
                    }

                    stopToUpdate.EstimatedDepartureTime = departureTimeSpan;
                    estimatedDepartureTime = stopToUpdate.EstimatedDepartureTime.Value;

                    _logger.LogInformation($"[REOPTIMIZE] Stop #{stopToUpdate.Id} ({stopToUpdate.RouteStop?.OrderType}) - Order: {stopToUpdate.Order}, Distance: {stopToUpdate.Distance:F2}km, ETA: {stopToUpdate.EstimatedArrivalTime}, Departure: {stopToUpdate.EstimatedDepartureTime}");
                }

                // Polyline güncelle
                journey.Polyline = finalRoute.OverviewPolyline?.Points;

                // Optimizasyon bayrağını kaldır
                journey.NeedsReoptimization = false;

                // BUGFIX S3.15: Commit transaction (all updates succeed together)
                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                _logger.LogInformation($"[REOPTIMIZE] ✅ BUGFIX S3.15: Journey #{request.JourneyId} reoptimized successfully with transaction");
        }
        catch (Exception transactionEx)
        {
                // BUGFIX S3.15: Rollback on any error - no partial updates
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(transactionEx, "[REOPTIMIZE] BUGFIX S3.15: Transaction rolled back due to error");
                throw;
        }

            // SignalR ile şoföre güncellemeyi bildir
            try
            {
                await _journeyHub.Clients.Group($"journey_{journey.Id}").SendAsync(
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
                    cancellationToken
                );

                _logger.LogInformation($"[REOPTIMIZE] SignalR notification sent to journey #{journey.Id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[REOPTIMIZE] Failed to send SignalR notification");
            }

            // Journey'i tekrar yükle
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

            return new JourneyResponse(journey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[REOPTIMIZE] Failed to reoptimize journey #{request.JourneyId}");
            throw new ApiException($"Optimizasyon başarısız: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Haversine formülü ile iki nokta arası mesafe hesapla (metre)
    /// </summary>
    private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371000; // Dünya yarıçapı metre
        var dLat = (lat2 - lat1) * Math.PI / 180;
        var dLon = (lon2 - lon1) * Math.PI / 180;
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c * 1.4; // 1.4x yol faktörü (gerçek yol mesafesi için)
    }
}
