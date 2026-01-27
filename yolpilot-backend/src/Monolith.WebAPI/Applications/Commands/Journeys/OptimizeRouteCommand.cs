using System.Text.Json;
using System.Text.Json.Serialization;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Data.Journeys;
using Monolith.WebAPI.External.Google;
using Monolith.WebAPI.Infrastructure;
using Monolith.WebAPI.Responses.Journeys;
using Monolith.WebAPI.Services.Members;
using Monolith.WebAPI.Services.Optimization;
using Monolith.WebAPI.Services.Subscription;
using System.Globalization;
using Microsoft.Extensions.Logging;

namespace Monolith.WebAPI.Applications.Commands.Journeys;

public class OptimizeRouteCommand : BaseAuthenticatedCommand<OptimizeRouteResponse>
{
    [JsonIgnore] public override bool RequiresDispatcher => false;
    [JsonIgnore] public override bool RequiresWorkspaceAccess => !IsTimeDeviationOptimization;
    [JsonIgnore] public int RouteId { get; set; }

    public string OptimizationMode { get; set; } = "distance";
    public bool IsTimeDeviationOptimization { get; set; } = false;
    public bool AvoidTolls { get; set; } = false;

    /// <summary>
    /// ✅ YENİ: True ise stop order'ı değiştirmez, sadece ETA hesaplar
    /// </summary>
    public bool PreserveOrder { get; set; } = false;
}

// YENI RESPONSE MODELI
public class OptimizeRouteResponse
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public int RouteId { get; set; }
    public List<RouteStopResponse> OptimizedStops { get; set; } = new();
    public List<ExcludedStopDto> ExcludedStops { get; set; } = new();
    public double TotalDistance { get; set; }
    public int TotalDuration { get; set; }
    public bool HasExclusions { get; set; }
    public bool Optimized { get; set; }
    public RouteEndDetailsResponse? EndDetails { get; set; }
}

public class RouteEndDetailsResponse
{
    public string Name { get; set; }
    public string Address { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string? EstimatedArrivalTime { get; set; }
}

public class ExcludedStopDto
{
    public RouteStopResponse Stop { get; set; }
    public string Reason { get; set; }
    public string TimeWindowConflict { get; set; }
}

// LOCAL RESULT CLASS (internal use only)
internal class OptimizationResultWithExclusions
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public List<int> OptimizedOrder { get; set; } = new();
    public List<ExcludedStopDto> ExcludedStops { get; set; } = new();
    public double TotalDistance { get; set; }
    public int TotalDuration { get; set; }
    public bool HasExclusions { get; set; }
}

public class OptimizeRouteCommandValidator : AbstractValidator<OptimizeRouteCommand>
{
    public OptimizeRouteCommandValidator()
    {
        RuleFor(x => x.RouteId).NotEmpty();
        RuleFor(x => x.OptimizationMode)
            .Must(x => x == null || x == "distance" || x == "duration")
            .WithMessage("OptimizationMode must be 'distance' or 'duration'");
    }
}

public class OptimizeRouteCommandHandler : BaseAuthenticatedCommandHandler<OptimizeRouteCommand, OptimizeRouteResponse>
{
    private readonly AppDbContext context;
    private readonly GoogleApiService googleApiService;
    private readonly ILogger<OptimizeRouteCommandHandler> logger;
    private readonly IServiceProvider serviceProvider;
    private readonly ISubscriptionService subscriptionService;

    public OptimizeRouteCommandHandler(
        AppDbContext context,
        GoogleApiService googleApiService,
        IUserService userService,
        ILogger<OptimizeRouteCommandHandler> logger,
        IServiceProvider serviceProvider,
        ISubscriptionService subscriptionService)
        : base(userService)
    {
        this.context = context;
        this.googleApiService = googleApiService;
        this.logger = logger;
        this.serviceProvider = serviceProvider;
        this.subscriptionService = subscriptionService;
    }

    override protected async Task<OptimizeRouteResponse> HandleCommand(OptimizeRouteCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("=== OPTIMIZE ROUTE START ===");
        logger.LogInformation($"RouteId: {request.RouteId}, Mode: {request.OptimizationMode}, IsTimeDeviationOptimization: {request.IsTimeDeviationOptimization}");
        
        // Time deviation optimization ise yetki kontrolünü atla
        if (request.IsTimeDeviationOptimization)
        {
            logger.LogInformation("Time deviation optimization - skipping dispatcher check");
        }
        else if (User.IsDriver && !User.IsDispatcher && !User.IsAdmin && !User.IsSuperAdmin)
        {
            logger.LogWarning($"User {User.Id} attempted to optimize route without dispatcher access");
            throw new ApiException("You are not authorized to perform this action. Dispatcher access required.", 403);
        }

        logger.LogInformation($"=== OPTIMIZE ROUTE START - RouteId: {request.RouteId} ===");
        logger.LogInformation($"AvoidTolls parameter: {request.AvoidTolls}");

        // PERFORMANCE: Will be modified in memory, so tracking is needed
        var route = await context.Routes
            .Include(x => x.Depot)
            .Include(x => x.Stops).ThenInclude(x => x.Customer)
            .Include(x => x.StartDetails)
            .Include(x => x.EndDetails)
            .Include(x => x.Driver)
            .Include(x => x.Vehicle)
            .Include(x => x.Workspace)
            .FirstOrDefaultAsync(x => x.Id == request.RouteId, cancellationToken);

        if (route is null)
            throw new ApiException("Route not found", 404);

        if (route?.Stops is null || route?.Stops?.Count == 0)
            throw new ApiException("Route stops not found", 404);

        // TIME WINDOW AUTO-COMPLETION VE VALIDATION
        logger.LogInformation("=== BEFORE OPTIMIZATION ===");
        foreach (var stop in route.Stops.Where(s => !s.IsExcluded).Take(5))
        {
            logger.LogInformation($"Stop {stop.Name}: ArriveBetweenStart={stop.ArriveBetweenStart}, ArriveBetweenEnd={stop.ArriveBetweenEnd}");
        }

        // Excluded olmayan stops'ları al
        var originalStops = route.Stops
            .Where(s => !s.IsExcluded)
            .OrderBy(s => s.Order)
            .ToList();

        var invalidStops = originalStops
            .Select(stop => new
            {
                Stop = stop,
                Latitude = stop.Customer != null ? stop.Customer.Latitude : stop.Latitude,
                Longitude = stop.Customer != null ? stop.Customer.Longitude : stop.Longitude
            })
            .Where(x => IsInvalidCoordinate(x.Latitude, x.Longitude))
            .ToList();

        if (invalidStops.Any())
        {
            var invalidStopNames = string.Join(", ",
                invalidStops
                    .Select(x => x.Stop.Name)
                    .Where(name => !string.IsNullOrWhiteSpace(name))
                    .Distinct()
                    .Take(5));

            logger.LogWarning("Route {RouteId} has {Count} stops with missing coordinates. Examples: {Stops}",
                route.Id,
                invalidStops.Count,
                invalidStopNames);

            var message = string.IsNullOrWhiteSpace(invalidStopNames)
                ? $"Koordinat bilgisi eksik {invalidStops.Count} durak var. Lutfen musteri kayitlarini guncelleyin."
                : $"Koordinat bilgisi eksik {invalidStops.Count} durak var. Ornek: {invalidStopNames}. Lutfen musteri kayitlarini guncelleyin.";

            throw new BadRequestException(message);
        }
        
        // Response object
        var response = new OptimizeRouteResponse
        {
            RouteId = route.Id,
            Success = true
        };

        logger.LogInformation($"BEFORE OPTIMIZE - Stop count: {originalStops.Count} (excluding {route.Stops.Count(s => s.IsExcluded)} excluded stops)");
        
        // Time window kontrolü
        var hasTimeWindows = CheckForTimeWindows(originalStops);
        logger.LogInformation($"Time windows detected: {hasTimeWindows}");

        // Subscription kontrolü
        if (hasTimeWindows && User.WorkspaceId > 0)
        {
            var canUseTimeWindows = await subscriptionService.CanUseTimeWindows(User.WorkspaceId);
            if (!canUseTimeWindows)
            {
                logger.LogWarning($"Workspace {User.WorkspaceId} attempted to use time windows without permission (Plan: Starter)");
                throw new ApiException("Zaman aralıklı teslimat özelliği planınızda bulunmamaktadır. Lütfen planınızı yükseltin.", 403);
            }
        }

        // 00:00 START TIME EDGE CASE KONTROLÜ - DÜZELTME: Value kaldırıldı
        if (route.StartDetails?.StartTime != null)
        {
            if (route.StartDetails.StartTime.TotalMinutes == 0)
            {
                logger.LogWarning("Route start time is 00:00, adjusting to 00:01");
                route.StartDetails.StartTime = TimeSpan.FromMinutes(1);
                context.Entry(route.StartDetails).State = EntityState.Modified;
                await context.SaveChangesAsync(cancellationToken);
            }
        }

        if (hasTimeWindows)
        {
            logger.LogInformation("Using OR-Tools optimization with time windows");
            var optimizationResult = await OptimizeWithOrToolsAndExclusions(route, originalStops, request.IsTimeDeviationOptimization, request.AvoidTolls, cancellationToken);

            response.HasExclusions = optimizationResult.HasExclusions;
            response.ExcludedStops = optimizationResult.ExcludedStops;
            response.Message = optimizationResult.Message;
            response.TotalDistance = optimizationResult.TotalDistance;
            response.TotalDuration = optimizationResult.TotalDuration;
            response.Optimized = optimizationResult.Success;
            response.Success = optimizationResult.Success;

            if (!optimizationResult.Success && optimizationResult.ExcludedStops.Count == originalStops.Count)
            {
                // Tüm duraklar exclude edildi
                response.Success = false;
                if (string.IsNullOrWhiteSpace(response.Message))
                {
                    response.Message = "Hicbir durak icin uygun zaman penceresi bulunamadi";
                }
            }

            // Time window var - ETA hesapla (API ile)
            if (response.Optimized)
            {
                try
                {
                    await CalculateEstimatedArrivalTimes(route, request.AvoidTolls, cancellationToken);
                    logger.LogInformation("ETA calculation completed successfully");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error during ETA calculation - continuing without ETA");
                }
            }
        }
        else
        {
            logger.LogInformation("No time windows, using Google Maps optimization");
            var directionsResponse = await OptimizeAndCalculateETA(route, request.OptimizationMode, request.AvoidTolls, request.PreserveOrder, cancellationToken);
            response.TotalDistance = route.TotalDistance ?? 0;
            response.TotalDuration = route.TotalDuration ?? 0;
            response.Optimized = true;

            // AvoidTolls ayarını sakla
            route.AvoidTolls = request.AvoidTolls;
        }

        // ETA hesaplama artık OptimizeAndCalculateETA içinde yapılıyor (time window yoksa)
        // Time window varsa yukarıda CalculateEstimatedArrivalTimes çağrılıyor

        // TIME WINDOW İHLAL KONTROLÜ - SADECE DB'DE KALAN VE EXCLUDED OLMAYAN STOPS İÇİN
        var timeWindowViolations = new List<string>();
        foreach (var stop in route.Stops.Where(s => !s.IsExcluded).OrderBy(x => x.Order))
        {
            TimeSpan? windowStart = null;
            TimeSpan? windowEnd = null;

            // Stop override kontrolü
            if (stop.ArriveBetweenStart.HasValue)
                windowStart = stop.ArriveBetweenStart.Value;
            else if (stop.Customer?.TimeWindowStart.HasValue == true)
                windowStart = stop.Customer.TimeWindowStart.Value;

            if (stop.ArriveBetweenEnd.HasValue)
                windowEnd = stop.ArriveBetweenEnd.Value;
            else if (stop.Customer?.TimeWindowEnd.HasValue == true)
                windowEnd = stop.Customer.TimeWindowEnd.Value;

            // Time window ihlali kontrolü
            if (windowStart.HasValue && windowEnd.HasValue && stop.EstimatedArrivalTime.HasValue)
            {
                if (stop.EstimatedArrivalTime.Value < windowStart || stop.EstimatedArrivalTime.Value > windowEnd)
                {
                    timeWindowViolations.Add($"{stop.Name}: Varış {stop.EstimatedArrivalTime.Value:hh\\:mm}, İstenen: {windowStart.Value:hh\\:mm}-{windowEnd.Value:hh\\:mm}");
                    logger.LogWarning($"Time window violation for {stop.Name}: Arrival {stop.EstimatedArrivalTime.Value:hh\\:mm}, Requested: {windowStart.Value:hh\\:mm}-{windowEnd.Value:hh\\:mm}");
                }
            }
        }

        if (timeWindowViolations.Any())
        {
            if (!string.IsNullOrEmpty(response.Message))
                response.Message += ". ";
            response.Message += "UYARI - Time window ihlalleri: " + string.Join("; ", timeWindowViolations);
        }

        logger.LogInformation("=== BEFORE FINAL SaveChanges ===");
        foreach (var stop in route.Stops.Where(s => !s.IsExcluded).Take(5))
        {
            logger.LogInformation($"Stop {stop.Name}: ArriveBetweenStart={stop.ArriveBetweenStart}, ArriveBetweenEnd={stop.ArriveBetweenEnd}");
        }

        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("=== AFTER FINAL SaveChanges - Before Detach ===");
        foreach (var stop in route.Stops.Where(s => !s.IsExcluded).Take(5))
        {
            logger.LogInformation($"Stop {stop.Name}: ArriveBetweenStart={stop.ArriveBetweenStart}, ArriveBetweenEnd={stop.ArriveBetweenEnd}");
        }

        context.Entry(route).State = EntityState.Detached;
        
        // Güncel route'u yükle
        route = await context.Routes
            .Include(x => x.Depot)
            .Include(x => x.Stops).ThenInclude(x => x.Customer)
            .Include(x => x.StartDetails)
            .Include(x => x.EndDetails)
            .Include(x => x.Driver)
            .Include(x => x.Vehicle)
            .Include(x => x.Workspace)
            .FirstOrDefaultAsync(x => x.Id == request.RouteId, cancellationToken);

        logger.LogInformation("=== AFTER ROUTE RELOAD ===");
        foreach (var stop in route.Stops.Where(s => !s.IsExcluded).Take(5))
        {
            logger.LogInformation($"Stop {stop.Name}: ArriveBetweenStart={stop.ArriveBetweenStart}, ArriveBetweenEnd={stop.ArriveBetweenEnd}");
        }

        // Optimized stops'ları response'a ekle (excluded olmayanlar)
        response.OptimizedStops = route.Stops
            .Where(s => !s.IsExcluded)
            .OrderBy(x => x.Order)
            .Select(x => new RouteStopResponse(x))
            .ToList();

        // EndDetails'i response'a ekle
        logger.LogInformation($"Building response - route.EndDetails is null: {route.EndDetails == null}");
        if (route.EndDetails != null)
        {
            logger.LogInformation($"EndDetails found - ETA: {route.EndDetails.EstimatedArrivalTime}");
            response.EndDetails = new RouteEndDetailsResponse
            {
                Name = route.EndDetails.Name,
                Address = route.EndDetails.Address,
                Latitude = route.EndDetails.Latitude,
                Longitude = route.EndDetails.Longitude,
                EstimatedArrivalTime = route.EndDetails.EstimatedArrivalTime?.ToString(@"hh\:mm")
            };
            logger.LogInformation($"EndDetails added to response: {response.EndDetails.EstimatedArrivalTime}");
        }
        else
        {
            logger.LogWarning("EndDetails is null, not adding to response");
        }

        logger.LogInformation($"AFTER OPTIMIZE - Optimized stops: {response.OptimizedStops.Count}, Excluded stops: {response.ExcludedStops.Count}");

        // FINAL DEBUG - Response object details right before return
        logger.LogInformation($"🔍 FINAL RESPONSE DEBUG:");
        logger.LogInformation($"  - Response.Success: {response.Success}");
        logger.LogInformation($"  - Response.EndDetails is null: {response.EndDetails == null}");
        if (response.EndDetails != null)
        {
            logger.LogInformation($"  - Response.EndDetails.Name: {response.EndDetails.Name}");
            logger.LogInformation($"  - Response.EndDetails.EstimatedArrivalTime: {response.EndDetails.EstimatedArrivalTime}");
        }

        // JSON SERIALIZATION TEST
        try
        {
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true,
                ReferenceHandler = ReferenceHandler.IgnoreCycles,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
            var serializedResponse = JsonSerializer.Serialize(response, jsonOptions);
            logger.LogInformation($"📄 SERIALIZED JSON RESPONSE: {serializedResponse}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to serialize response for debugging");
        }
        logger.LogInformation("=== OPTIMIZE ROUTE END ===");

        return response;
    }

    // TIME WINDOW VALIDATION VE AUTO-COMPLETION - DÜZELTME: Reflection kullanımı

    private async Task<OptimizationResultWithExclusions> OptimizeWithOrToolsAndExclusions(
        Data.Journeys.Route route,
        List<RouteStop> originalStops,
        bool isTimeDeviationOptimization,
        bool avoidTolls,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("=== OR-TOOLS OPTIMIZE WITH EXCLUSIONS START ===");
        logger.LogInformation($"IsTimeDeviationOptimization: {isTimeDeviationOptimization}");
        
        var optimizationService = serviceProvider.GetService<IOptimizationService>();
        if (optimizationService == null)
        {
            var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
            var orToolsLogger = loggerFactory.CreateLogger<OrToolsOptimizationService>();
            optimizationService = new OrToolsOptimizationService(orToolsLogger, googleApiService);
        }

        var stops = originalStops.Select(s =>
        {
            TimeSpan? windowStart = null;
            TimeSpan? windowEnd = null;

            if (s.ArriveBetweenStart.HasValue)
            {
                windowStart = s.ArriveBetweenStart.Value;
                logger.LogInformation($"Stop {s.Name}: Using override TimeWindowStart: {windowStart}");
            }
            else if (s.Customer?.TimeWindowStart.HasValue == true)
            {
                windowStart = s.Customer.TimeWindowStart.Value;
                logger.LogInformation($"Stop {s.Name}: Using customer TimeWindowStart: {windowStart}");
            }

            if (s.ArriveBetweenEnd.HasValue)
            {
                windowEnd = s.ArriveBetweenEnd.Value;
                logger.LogInformation($"Stop {s.Name}: Using override TimeWindowEnd: {windowEnd}");
            }
            else if (s.Customer?.TimeWindowEnd.HasValue == true)
            {
                windowEnd = s.Customer.TimeWindowEnd.Value;
                logger.LogInformation($"Stop {s.Name}: Using customer TimeWindowEnd: {windowEnd}");
            }

            return new OptimizationStop
            {
                Id = s.Id,
                Name = s.Name,
                Latitude = s.Customer?.Latitude ?? s.Latitude,
                Longitude = s.Customer?.Longitude ?? s.Longitude,
                ServiceTimeMinutes = s.ServiceTime.HasValue
                    ? (int)s.ServiceTime.Value.TotalMinutes
                    : (s.Customer?.EstimatedServiceTime ?? 10),
                TimeWindowStart = windowStart,
                TimeWindowEnd = windowEnd,
                CustomerId = s.CustomerId,
                OrderType = s.OrderType
            };
        }).ToList();
        
        var routeStartTime = route.StartDetails?.StartTime ?? new TimeSpan(9, 0, 0);
        var result = await optimizationService.OptimizeWithExclusions(
            route.DepotId,
            route.Depot.Latitude,
            route.Depot.Longitude,
            stops,
            routeStartTime,
            null);

        var returnResult = new OptimizationResultWithExclusions
        {
            Success = result.Success,
            Message = result.Message,
            TotalDistance = result.TotalDistance,
            TotalDuration = result.TotalDuration,
            HasExclusions = result.ExcludedStops.Any(),
            ExcludedStops = new List<ExcludedStopDto>()
        };

        if (result.Success)
        {
            // Transaction kullanarak atomik işlem yap
            using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                // 1. ID bazlı eşitleme - index kullanma!
                var includedStopIds = new HashSet<int>();
                foreach (var optimizedIndex in result.OptimizedOrder)
                {
                    var optimizedStop = originalStops[optimizedIndex];
                    includedStopIds.Add(optimizedStop.Id);
                }

                // 2. Excluded stops'ları işaretle veya sil
                var stopsToExclude = originalStops
                    .Where(s => !includedStopIds.Contains(s.Id))
                    .ToList();

                if (isTimeDeviationOptimization)
                {
                    // SOFT EXCLUDE - Mobil sapma optimizasyonu için sadece işaretle
                    foreach (var stopToExclude in stopsToExclude)
                    {
                        stopToExclude.IsExcluded = true;
                        stopToExclude.ExclusionReason = "Time window constraint - excluded during deviation optimization";
                        stopToExclude.Order = 0; // Sıralamadan çıkar
                        context.Entry(stopToExclude).State = EntityState.Modified;
                        
                        logger.LogInformation($"Soft excluding stop: {stopToExclude.Name} (ID: {stopToExclude.Id})");
                        
                        // Response için excluded stops listesine ekle
                        var excludedInfo = result.ExcludedStops.FirstOrDefault(e => e.Stop.Id == stopToExclude.Id);
                        returnResult.ExcludedStops.Add(new ExcludedStopDto
                        {
                            Stop = new RouteStopResponse(stopToExclude),
                            Reason = excludedInfo?.Reason ?? "Belirtilen zaman aralığında teslimat yapılamıyor",
                            TimeWindowConflict = excludedInfo?.TimeWindowConflict
                        });
                    }
                }
                else
                {
                    // HARD DELETE - Web planlama için mevcut mantık
                    foreach (var stopToRemove in stopsToExclude)
                    {
                        context.RouteStops.Remove(stopToRemove);
                        logger.LogInformation($"Removing excluded stop from DB: {stopToRemove.Name} (ID: {stopToRemove.Id})");
                        
                        // Response için excluded stops listesine ekle
                        var excludedInfo = result.ExcludedStops.FirstOrDefault(e => e.Stop.Id == stopToRemove.Id);
                        returnResult.ExcludedStops.Add(new ExcludedStopDto
                        {
                            Stop = new RouteStopResponse(stopToRemove),
                            Reason = excludedInfo?.Reason ?? "Belirtilen zaman aralığında teslimat yapılamıyor",
                            TimeWindowConflict = excludedInfo?.TimeWindowConflict
                        });
                    }
                }

                // 3. Included stops'ların order'larını güncelle (1'den başlat)
                var orderedStops = result.OptimizedOrder
                    .Select((idx, newOrder) => new {
                        Stop = originalStops[idx],
                        NewOrder = newOrder + 1
                    })
                    .ToList();

                foreach (var item in orderedStops)
                {
                    var oldOrder = item.Stop.Order;
                    item.Stop.Order = item.NewOrder;

                    // Time window değerlerini korumak için explicitly mark edelim
                    context.Entry(item.Stop).State = EntityState.Modified;
                    context.Entry(item.Stop).Property(x => x.ArriveBetweenStart).IsModified = true;
                    context.Entry(item.Stop).Property(x => x.ArriveBetweenEnd).IsModified = true;

                    logger.LogInformation($"Reordering: {item.Stop.Name} from order {oldOrder} to {item.NewOrder} (TimeWindow: {item.Stop.ArriveBetweenStart}-{item.Stop.ArriveBetweenEnd})");
                }

                // 4. Route totals güncelle
                route.TotalDeliveries = orderedStops.Count;
                route.TotalDistance = result.TotalDistance;
                route.TotalDuration = result.TotalDuration;
                route.Optimized = true;

                // AvoidTolls ayarını sakla
                route.AvoidTolls = avoidTolls;
                
                try
                {
                    var origin = $"{route.Depot.Latitude},{route.Depot.Longitude}";
                    var destination = origin; // Depoya geri dön
                    
                    // Optimize edilmiş sıradaki durakları kullan
                    var waypoints = orderedStops
                        .OrderBy(s => s.NewOrder) // Yeni order'a göre sırala
                        .Select(s => $"{s.Stop.Customer?.Latitude ?? s.Stop.Latitude},{s.Stop.Customer?.Longitude ?? s.Stop.Longitude}")
                        .ToList();
                    
                    var directionsResponse = await googleApiService.GetDirections(origin, destination, waypoints, false, avoidTolls);
                    
                    if (directionsResponse?.Routes?.FirstOrDefault()?.OverviewPolyline != null)
                    {
                        route.Polyline = directionsResponse.Routes.First().OverviewPolyline.Points;
                        logger.LogInformation($"Polyline added to route from Google, length: {route.Polyline?.Length ?? 0}");
                    }
                }
                catch (Exception ex)
                {
                    logger.LogWarning($"Could not get polyline for OR-Tools optimized route: {ex.Message}");
                    // Polyline alamasak bile optimizasyona devam et
                }

                // Depoyu son durak olarak database'e ekle (OR-Tools optimization için de) - Sadece yoksa
                var existingDepotStop = route.Stops.FirstOrDefault(s =>
                    s.CustomerId == null &&
                    s.Latitude == route.Depot.Latitude &&
                    s.Longitude == route.Depot.Longitude &&
                    s.OrderType == OrderType.Last &&
                    !s.IsExcluded);

                if (existingDepotStop == null)
                {
                    var depotStop = new Data.Journeys.RouteStop();
                    context.Entry(depotStop).State = EntityState.Added;
                    context.Entry(depotStop).CurrentValues.SetValues(new
                    {
                        Name = route.Depot.Name + " (Dönüş)",
                        Address = route.Depot.Address,
                        Latitude = route.Depot.Latitude,
                        Longitude = route.Depot.Longitude,
                        OrderType = OrderType.Last,
                        Type = 1, // Delivery
                        ServiceTime = TimeSpan.FromMinutes(5),
                        RouteId = route.Id,
                        CustomerId = (int?)null, // Depot için customer yok
                        ContactFullName = route.Depot.Name,
                        ContactPhone = "",
                        ContactEmail = "",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        Order = orderedStops.Count + 1,
                        EstimatedArrivalTime = (TimeSpan?)null,
                        EstimatedDepartureTime = (TimeSpan?)null,
                        ArriveBetweenStart = (TimeSpan?)null,
                        ArriveBetweenEnd = (TimeSpan?)null,
                        ExclusionReason = "",
                        IsDeleted = false,
                        IsExcluded = false,
                        Notes = "",
                        PhotoRequired = false,
                        ProofOfDeliveryRequired = false,
                        SignatureRequired = false
                    });
                    logger.LogInformation("Added depot as final stop for OR-Tools optimization: {DepotName}", route.Depot.Name);
                }
                else
                {
                    // Var olan depot stop'un order'ını güncelle
                    existingDepotStop.Order = orderedStops.Count + 1;
                    context.Entry(existingDepotStop).State = EntityState.Modified;
                    logger.LogInformation("Updated existing depot stop order to: {Order}", orderedStops.Count + 1);
                }

                context.Routes.Update(route);

                logger.LogInformation($"OR-Tools optimization successful. Distance: {result.TotalDistance}km, Duration: {result.TotalDuration}min");

                await context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                logger.LogError(ex, "Error during optimization transaction");
                throw;
            }
        }
        
        if (returnResult.ExcludedStops.Count == 0 && result.ExcludedStops != null && result.ExcludedStops.Any())
        {
            foreach (var excluded in result.ExcludedStops)
            {
                var sourceStop = originalStops.FirstOrDefault(s => s.Id == excluded.Stop.Id);
                if (sourceStop == null)
                {
                    continue;
                }

                returnResult.ExcludedStops.Add(new ExcludedStopDto
                {
                    Stop = new RouteStopResponse(sourceStop),
                    Reason = excluded.Reason ?? "Zaman penceresi uyumsuzlugu",
                    TimeWindowConflict = excluded.TimeWindowConflict
                });
            }
        }

        logger.LogInformation("=== OR-TOOLS OPTIMIZE WITH EXCLUSIONS END ===");
        return returnResult;
    }

    private bool CheckForTimeWindows(List<RouteStop> stops)
    {
        logger.LogInformation($"=== CheckForTimeWindows START - Checking {stops.Count} stops ===");

        foreach (var stop in stops)
        {
            logger.LogInformation($"Checking stop: {stop.Name} (CustomerId: {stop.CustomerId})");

            logger.LogInformation($"Stop {stop.Name}: ArriveBetweenStart = {stop.ArriveBetweenStart}, ArriveBetweenEnd = {stop.ArriveBetweenEnd}");

            if (stop.ArriveBetweenStart.HasValue || stop.ArriveBetweenEnd.HasValue)
            {
                logger.LogInformation($"Stop {stop.Name}: Has override time windows - Start: {stop.ArriveBetweenStart}, End: {stop.ArriveBetweenEnd}");
                return true;
            }

            if (stop.Customer != null)
            {
                logger.LogInformation($"Stop {stop.Name}: Customer found - TimeWindowStart: '{stop.Customer.TimeWindowStart}', TimeWindowEnd: '{stop.Customer.TimeWindowEnd}'");

                if (stop.Customer.TimeWindowStart.HasValue)
                {
                    logger.LogInformation($"Stop {stop.Name}: Has customer TimeWindowStart: {stop.Customer.TimeWindowStart}");
                    return true;
                }

                if (stop.Customer.TimeWindowEnd.HasValue)
                {
                    logger.LogInformation($"Stop {stop.Name}: Has customer TimeWindowEnd: {stop.Customer.TimeWindowEnd}");
                    return true;
                }
            }
            else
            {
                logger.LogInformation($"Stop {stop.Name}: No customer data loaded");
            }
        }

        logger.LogInformation("=== CheckForTimeWindows END - No time windows found ===");
        return false;
    }

    private static bool TryParseTime(string? s, out TimeSpan ts)
    {
        if (!string.IsNullOrWhiteSpace(s))
        {
            if (TimeSpan.TryParseExact(s.Trim(), new[] { @"hh\:mm", @"hh\:mm\:ss" }, CultureInfo.InvariantCulture, out ts))
                return true;
            if (TimeSpan.TryParse(s, CultureInfo.InvariantCulture, out ts))
                return true;
        }
        ts = default;
        return false;
    }

    // DÜZELTME: Gece yarısı geçişi için akıllı ETA hesaplama
    private async Task CalculateEstimatedArrivalTimes(Data.Journeys.Route route, bool avoidTolls, CancellationToken cancellationToken)
    {
        logger.LogInformation("=== CALCULATING ESTIMATED ARRIVAL TIMES ===");

        // ✅ BUGFIX: Eğer journey başlamışsa, şu anki zamanı kullan ve sadece kalan durakları hesapla
        var journey = await context.Journeys
            .Include(j => j.Stops)
            .AsNoTracking()
            .FirstOrDefaultAsync(j => j.RouteId == route.Id, cancellationToken);

        // Önce orderedStops'u oluştur (index hesaplama için gerekli)
        var orderedStops = route.Stops
            .Where(s => !s.IsExcluded)
            .OrderBy(x => x.Order)
            .ToList();

        TimeSpan startTime;
        int startFromStopIndex = 0;
        string startLocation;
        var depotLocation = $"{route.Depot.Latitude.ToString(CultureInfo.InvariantCulture)},{route.Depot.Longitude.ToString(CultureInfo.InvariantCulture)}";

        if (journey?.StartedAt != null)
        {
            // Son tamamlanan durağı bul
            var completedJourneyStops = journey.Stops
                .Where(s => s.CheckOutTime.HasValue)
                .OrderByDescending(s => s.Order)
                .ToList();

            if (completedJourneyStops.Any())
            {
                var lastCompletedStop = completedJourneyStops.First();

                // ✅ FIX 1: orderedStops üzerinden doğru index'i bul
                var lastCompletedRouteStop = orderedStops.FirstOrDefault(rs => rs.Order == lastCompletedStop.Order);

                if (lastCompletedRouteStop != null)
                {
                    // ✅ FIX 2: CheckOutTime kullan, UtcNow değil!
                    startTime = lastCompletedStop.CheckOutTime.Value.TimeOfDay;

                    // Son tamamlanan durağın konumundan başla
                    startLocation = $"{lastCompletedRouteStop.Latitude.ToString(CultureInfo.InvariantCulture)},{lastCompletedRouteStop.Longitude.ToString(CultureInfo.InvariantCulture)}";

                    // ✅ FIX 1: orderedStops üzerinden index hesapla
                    startFromStopIndex = orderedStops.IndexOf(lastCompletedRouteStop) + 1;

                    logger.LogInformation($"Journey in progress, last completed stop: {lastCompletedStop.Order}, checkout time: {startTime}, starting from index: {startFromStopIndex}");
                }
                else
                {
                    // Fallback: CheckOutTime veya şu anki zaman
                    startTime = lastCompletedStop.CheckOutTime?.TimeOfDay ?? DateTime.UtcNow.TimeOfDay;
                    startLocation = depotLocation;
                    logger.LogInformation($"Journey started, using checkout time from last stop: {startTime}");
                }
            }
            else
            {
                // Hiç durak tamamlanmamış, şu anki zamanı kullan
                startTime = DateTime.UtcNow.TimeOfDay;
                startLocation = depotLocation;
                logger.LogInformation($"Journey started, no stops completed yet, using current time: {startTime}");
            }
        }
        else
        {
            // Journey henüz başlamamış - planlanan başlangıç zamanını kullan
            startTime = route.StartDetails?.StartTime ?? new TimeSpan(8, 0, 0);
            startLocation = depotLocation;
            logger.LogInformation($"Journey not started, using planned start time: {startTime}");
        }

        var currentDepartureTime = startTime;

        logger.LogInformation($"Route start time: {startTime}, calculating ETA for {orderedStops.Count - startFromStopIndex} remaining stops (starting from index {startFromStopIndex})");

        // Gece yarısı geçişini takip et
        int dayOffset = 0; // Kaç gün ileri gittik

        // ⭐ OPTIMIZATION: Tek API çağrısı ile tüm duraklar arası süreleri al (depot return dahil)
        try
        {
            // Waypoints listesi oluştur (startFromStopIndex'ten sonraki tüm duraklar)
            var remainingStops = orderedStops.Skip(startFromStopIndex).ToList();

            // ⭐ Son durak HARİÇ waypoints oluştur (son durak destination olacak, depot eklenecek)
            var waypoints = remainingStops
                .Take(remainingStops.Count > 0 ? remainingStops.Count - 1 : 0)
                .Select(s => $"{s.Latitude.ToString(CultureInfo.InvariantCulture)},{s.Longitude.ToString(CultureInfo.InvariantCulture)}")
                .ToList();

            // ⭐ DEPOT RETURN: Son müşteriyi waypoint olarak ekle, destination = depot
            if (remainingStops.Count > 0)
            {
                var lastCustomerStop = remainingStops.Last();
                waypoints.Add($"{lastCustomerStop.Latitude.ToString(CultureInfo.InvariantCulture)},{lastCustomerStop.Longitude.ToString(CultureInfo.InvariantCulture)}");
            }

            // Destination ALWAYS depot (for return leg)
            string destination = depotLocation;

            // Departure time hesapla (traffic-aware routing için)
            var journeyDate = route.Date;
            var departureDateTime = journeyDate.Date.Add(startTime);

            logger.LogInformation($"⭐ Making SINGLE API call for {remainingStops.Count} stops + depot return");
            logger.LogInformation($"⭐ Origin: {startLocation}");
            logger.LogInformation($"⭐ Waypoints: {waypoints.Count} (all {remainingStops.Count} customer stops)");
            logger.LogInformation($"⭐ Destination: {destination} (depot - for return leg)");
            logger.LogInformation($"⭐ Using traffic-aware routing with departure time: {departureDateTime:yyyy-MM-dd HH:mm:ss}");
            logger.LogInformation($"⭐ Expected legs: {remainingStops.Count + 1} ({remainingStops.Count} customers + 1 depot return)");

            // ⭐ TEK API çağrısı - tüm duraklar + depot return için
            var directionsResponse = await googleApiService.GetDirections(
                startLocation,
                destination, // Depot (circular route)
                waypoints,
                optimize: false, // Zaten optimize edilmiş sıra
                avoidTolls,
                departureTime: departureDateTime // Traffic-aware
            );

            if (directionsResponse?.Routes?.FirstOrDefault()?.Legs != null)
            {
                var legs = directionsResponse.Routes.First().Legs;
                logger.LogInformation($"✅ Received {legs.Count} legs from Google Directions API (includes depot return)");

                // Her durak için legs array'inden süreleri al
                // Circular route: legs[0..N-1] = müşteri durakları, legs[N] = depot return
                for (int i = 0; i < remainingStops.Count && i < legs.Count; i++)
                {
                    var stop = remainingStops[i];
                    var leg = legs[i];
                    var travelTimeMinutes = (int)(leg.Duration.Value / 60);
                    var distanceKm = (leg.Distance?.Value ?? 0) / 1000.0;

                    var arrivalTime = currentDepartureTime.Add(TimeSpan.FromMinutes(travelTimeMinutes));
                    var serviceTime = stop.ServiceTime ?? route.Workspace?.DefaultServiceTime ?? TimeSpan.FromMinutes(10);
                    var departureTime = arrivalTime.Add(serviceTime);

                    // SQL TIME veri tipi için 24 saat sınırına uygun hale getir
                    TimeSpan sqlArrivalTime = arrivalTime;
                    TimeSpan sqlDepartureTime = departureTime;

                    // 24 saati aşıyorsa, gece yarısından sonraki saate çevir
                    if (sqlArrivalTime.TotalHours >= 24)
                    {
                        int days = (int)(sqlArrivalTime.TotalHours / 24);
                        sqlArrivalTime = sqlArrivalTime.Subtract(TimeSpan.FromDays(days));
                        dayOffset += days;
                    }

                    if (sqlDepartureTime.TotalHours >= 24)
                    {
                        int days = (int)(sqlDepartureTime.TotalHours / 24);
                        sqlDepartureTime = sqlDepartureTime.Subtract(TimeSpan.FromDays(days));
                    }

                    // Negatif değer kontrolü (gece yarısı geçişlerinde olabilir)
                    if (sqlArrivalTime < TimeSpan.Zero)
                    {
                        sqlArrivalTime = sqlArrivalTime.Add(TimeSpan.FromDays(1));
                    }
                    if (sqlDepartureTime < TimeSpan.Zero)
                    {
                        sqlDepartureTime = sqlDepartureTime.Add(TimeSpan.FromDays(1));
                    }

                    // SQL'in kabul edeceği aralıkta olduğundan emin ol
                    if (sqlArrivalTime >= TimeSpan.FromHours(24))
                    {
                        sqlArrivalTime = TimeSpan.FromHours(23).Add(TimeSpan.FromMinutes(59));
                    }
                    if (sqlDepartureTime >= TimeSpan.FromHours(24))
                    {
                        sqlDepartureTime = TimeSpan.FromHours(23).Add(TimeSpan.FromMinutes(59));
                    }

                    stop.EstimatedArrivalTime = sqlArrivalTime;
                    stop.EstimatedDepartureTime = sqlDepartureTime;
                    // Note: Distance is stored in JourneyStop, not RouteStop (template)
                    context.Entry(stop).State = EntityState.Modified;

                    logger.LogInformation($"Stop {stop.Order} ({stop.Name}):");
                    logger.LogInformation($"  - Travel time from previous: {travelTimeMinutes} min (Google API with traffic)");
                    logger.LogInformation($"  - Distance: {distanceKm:F2} km (Google API)");
                    logger.LogInformation($"  - Service time: {serviceTime.TotalMinutes} min");
                    logger.LogInformation($"  - Actual arrival: {arrivalTime} (Day offset: {dayOffset})");
                    logger.LogInformation($"  - SQL arrival time: {sqlArrivalTime}");
                    logger.LogInformation($"  - SQL departure time: {sqlDepartureTime}");

                    // Gerçek hesaplama için orijinal değerleri koru
                    currentDepartureTime = departureTime;
                }

                // ⭐ DEPOT RETURN: Son leg depot'a dönüş (legs[N] = son müşteri → depot)
                if (legs.Count == remainingStops.Count + 1)
                {
                    var depotReturnLeg = legs[legs.Count - 1]; // Son leg
                    var returnTimeMinutes = (int)(depotReturnLeg.Duration.Value / 60);

                    logger.LogInformation($"⭐ Processing DEPOT RETURN leg (leg #{legs.Count - 1})");

                    var depotArrivalTime = currentDepartureTime.Add(TimeSpan.FromMinutes(returnTimeMinutes));

                    // SQL TIME veri tipi için 24 saat sınırına uygun hale getir
                    TimeSpan sqlDepotArrivalTime = depotArrivalTime;
                    if (sqlDepotArrivalTime.TotalHours >= 24)
                    {
                        int days = (int)(sqlDepotArrivalTime.TotalHours / 24);
                        sqlDepotArrivalTime = sqlDepotArrivalTime.Subtract(TimeSpan.FromDays(days));
                        dayOffset += days;
                    }

                    if (sqlDepotArrivalTime < TimeSpan.Zero)
                    {
                        sqlDepotArrivalTime = sqlDepotArrivalTime.Add(TimeSpan.FromDays(1));
                    }

                    if (sqlDepotArrivalTime >= TimeSpan.FromHours(24))
                    {
                        sqlDepotArrivalTime = TimeSpan.FromHours(23).Add(TimeSpan.FromMinutes(59));
                    }

                    // Route'a depo geri dönüş bilgilerini ekle
                    if (route.EndDetails == null)
                    {
                        route.EndDetails = new Data.Journeys.RouteEndDetails
                        {
                            Name = route.Depot.Name,
                            Address = route.Depot.Address,
                            Latitude = route.Depot.Latitude,
                            Longitude = route.Depot.Longitude,
                            RouteId = route.Id
                        };
                    }

                    route.EndDetails.EstimatedArrivalTime = sqlDepotArrivalTime;
                    context.Entry(route.EndDetails).State = route.EndDetails.Id == 0 ? EntityState.Added : EntityState.Modified;

                    logger.LogInformation($"⭐ DEPOT RETURN:");
                    logger.LogInformation($"  - Travel time from last stop: {returnTimeMinutes} min (Google API with traffic)");
                    logger.LogInformation($"  - Distance: {(depotReturnLeg.Distance?.Value ?? 0) / 1000.0:F2} km (Google API)");
                    logger.LogInformation($"  - Actual depot arrival: {depotArrivalTime} (Day offset: {dayOffset})");
                    logger.LogInformation($"  - SQL depot arrival time: {sqlDepotArrivalTime}");
                }
                else
                {
                    logger.LogWarning($"⚠️ Expected {remainingStops.Count + 1} legs (including depot return), but got {legs.Count}");
                    logger.LogWarning($"⚠️ Depot return leg is MISSING! Distance/duration will be INCORRECT!");
                    logger.LogWarning($"⚠️ This causes the bug where re-optimization shows less distance.");
                }
            }
            else
            {
                logger.LogError("Google Directions API returned no routes or legs");
                throw new Exception("Google Directions API returned no valid route data");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get directions from Google API, using fallback calculation");

            // ❌ FALLBACK: API başarısız olursa eski yöntemi kullan
            for (int i = startFromStopIndex; i < orderedStops.Count; i++)
            {
                var stop = orderedStops[i];

                // Varsayılan değerlerle devam et
                var arrivalTime = currentDepartureTime.Add(TimeSpan.FromMinutes(15));
                var serviceTime = stop.ServiceTime ?? route.Workspace?.DefaultServiceTime ?? TimeSpan.FromMinutes(10);
                var departureTime = arrivalTime.Add(serviceTime);
                
                TimeSpan sqlArrivalTime = arrivalTime;
                TimeSpan sqlDepartureTime = departureTime;
                
                // 24 saat düzeltmesi
                if (sqlArrivalTime.TotalHours >= 24)
                {
                    int days = (int)(sqlArrivalTime.TotalHours / 24);
                    sqlArrivalTime = sqlArrivalTime.Subtract(TimeSpan.FromDays(days));
                }
                
                if (sqlDepartureTime.TotalHours >= 24)
                {
                    int days = (int)(sqlDepartureTime.TotalHours / 24);
                    sqlDepartureTime = sqlDepartureTime.Subtract(TimeSpan.FromDays(days));
                }
                
                // SQL sınırları içinde tut
                if (sqlArrivalTime >= TimeSpan.FromHours(24))
                {
                    sqlArrivalTime = TimeSpan.FromHours(23).Add(TimeSpan.FromMinutes(59));
                }
                if (sqlDepartureTime >= TimeSpan.FromHours(24))
                {
                    sqlDepartureTime = TimeSpan.FromHours(23).Add(TimeSpan.FromMinutes(59));
                }
                
                stop.EstimatedArrivalTime = sqlArrivalTime;
                stop.EstimatedDepartureTime = sqlDepartureTime;
                context.Entry(stop).State = EntityState.Modified;

                currentDepartureTime = departureTime;
            }

            // ❌ FALLBACK DEPOT RETURN: Varsayılan 20 dakika
            var estimatedReturnTime = currentDepartureTime.Add(TimeSpan.FromMinutes(20));
            TimeSpan sqlDepotArrivalTime = estimatedReturnTime;

            if (sqlDepotArrivalTime.TotalHours >= 24)
            {
                int days = (int)(sqlDepotArrivalTime.TotalHours / 24);
                sqlDepotArrivalTime = sqlDepotArrivalTime.Subtract(TimeSpan.FromDays(days));
            }

            if (sqlDepotArrivalTime >= TimeSpan.FromHours(24))
            {
                sqlDepotArrivalTime = TimeSpan.FromHours(23).Add(TimeSpan.FromMinutes(59));
            }

            if (route.EndDetails == null)
            {
                route.EndDetails = new Data.Journeys.RouteEndDetails
                {
                    Name = route.Depot.Name,
                    Address = route.Depot.Address,
                    Latitude = route.Depot.Latitude,
                    Longitude = route.Depot.Longitude,
                    RouteId = route.Id
                };
            }

            route.EndDetails.EstimatedArrivalTime = sqlDepotArrivalTime;
            context.Entry(route.EndDetails).State = route.EndDetails.Id == 0 ? EntityState.Added : EntityState.Modified;

            logger.LogInformation($"FALLBACK Depot return: ~20 min, arrival: {sqlDepotArrivalTime}");
        }

        // ⚠️ DELETE OLD DEPOT RETURN CODE: Now handled inside try/catch blocks
        // The following code (lines 991-1103) was the old depot return calculation
        // It's now replaced by circular route in the try block, and fallback in catch block
        /*
        if (orderedStops.Count > 0)
        {
            var lastStop = orderedStops.Last();
            var lastStopLocation = $"{lastStop.Latitude.ToString(CultureInfo.InvariantCulture)},{lastStop.Longitude.ToString(CultureInfo.InvariantCulture)}";

            logger.LogInformation($"=== CALCULATING DEPOT RETURN ===");
            logger.LogInformation($"Last stop: {lastStop.Name}");
            logger.LogInformation($"Last stop location: {lastStopLocation}");
            logger.LogInformation($"Depot location: {depotLocation}");
            logger.LogInformation($"Current departure time from last stop: {currentDepartureTime}");

            try
            {
                logger.LogInformation("Calling Google API for depot return route...");
                var returnDirections = await googleApiService.GetDirections(lastStopLocation, depotLocation, new List<string>(), false, avoidTolls);
                logger.LogInformation($"Google API returned: {returnDirections != null}");
                if (returnDirections?.Routes?.FirstOrDefault()?.Legs?.FirstOrDefault() != null)
                {
                    logger.LogInformation("Valid return route found from Google API");
                    var returnLeg = returnDirections.Routes.First().Legs.First();
                    var returnTimeMinutes = (int)(returnLeg.Duration.Value / 60);
                    logger.LogInformation($"Return travel time: {returnTimeMinutes} minutes");

                    var depotArrivalTime = currentDepartureTime.Add(TimeSpan.FromMinutes(returnTimeMinutes));

                    // SQL TIME veri tipi için 24 saat sınırına uygun hale getir
                    TimeSpan sqlDepotArrivalTime = depotArrivalTime;
                    if (sqlDepotArrivalTime.TotalHours >= 24)
                    {
                        int days = (int)(sqlDepotArrivalTime.TotalHours / 24);
                        sqlDepotArrivalTime = sqlDepotArrivalTime.Subtract(TimeSpan.FromDays(days));
                        dayOffset += days;
                    }

                    if (sqlDepotArrivalTime < TimeSpan.Zero)
                    {
                        sqlDepotArrivalTime = sqlDepotArrivalTime.Add(TimeSpan.FromDays(1));
                    }

                    if (sqlDepotArrivalTime >= TimeSpan.FromHours(24))
                    {
                        sqlDepotArrivalTime = TimeSpan.FromHours(23).Add(TimeSpan.FromMinutes(59));
                    }

                    // Route'a depo geri dönüş bilgilerini ekle
                    if (route.EndDetails == null)
                    {
                        route.EndDetails = new Data.Journeys.RouteEndDetails
                        {
                            Name = route.Depot.Name,
                            Address = route.Depot.Address,
                            Latitude = route.Depot.Latitude,
                            Longitude = route.Depot.Longitude,
                            RouteId = route.Id
                        };
                    }

                    route.EndDetails.EstimatedArrivalTime = sqlDepotArrivalTime;
                    context.Entry(route.EndDetails).State = route.EndDetails.Id == 0 ? EntityState.Added : EntityState.Modified;

                    logger.LogInformation($"Return to depot:");
                    logger.LogInformation($"  - Travel time from last stop: {returnTimeMinutes} min");
                    logger.LogInformation($"  - Actual depot arrival: {depotArrivalTime} (Day offset: {dayOffset})");
                    logger.LogInformation($"  - SQL depot arrival time: {sqlDepotArrivalTime}");

                    // NOT: TotalDuration zaten OR-Tools tarafından doğru hesaplanmış (satır 535)
                    // Burada tekrar hesaplama - OR-Tools'un değerini kullan
                    logger.LogInformation("Depot return calculation completed successfully");
                }
                else
                {
                    logger.LogWarning("Could not get valid return route from Google API - no routes or legs found");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Exception during depot return calculation: {ex.Message}");

                // Varsayılan değerlerle devam et
                var estimatedReturnTime = currentDepartureTime.Add(TimeSpan.FromMinutes(20)); // 20 dk varsayılan
                TimeSpan sqlDepotArrivalTime = estimatedReturnTime;

                if (sqlDepotArrivalTime.TotalHours >= 24)
                {
                    int days = (int)(sqlDepotArrivalTime.TotalHours / 24);
                    sqlDepotArrivalTime = sqlDepotArrivalTime.Subtract(TimeSpan.FromDays(days));
                }

                if (sqlDepotArrivalTime >= TimeSpan.FromHours(24))
                {
                    sqlDepotArrivalTime = TimeSpan.FromHours(23).Add(TimeSpan.FromMinutes(59));
                }

                if (route.EndDetails == null)
                {
                    route.EndDetails = new Data.Journeys.RouteEndDetails
                    {
                        Name = route.Depot.Name,
                        Address = route.Depot.Address,
                        Latitude = route.Depot.Latitude,
                        Longitude = route.Depot.Longitude,
                        RouteId = route.Id
                    };
                }

                route.EndDetails.EstimatedArrivalTime = sqlDepotArrivalTime;
                context.Entry(route.EndDetails).State = route.EndDetails.Id == 0 ? EntityState.Added : EntityState.Modified;

                // NOT: TotalDuration zaten OR-Tools tarafından doğru hesaplanmış (satır 535)
                // Hata durumunda bile OR-Tools'un değerini kullan
            }
        }
        */
        // END OF OLD DEPOT RETURN CODE - Now replaced with circular route approach

        context.Entry(route).State = EntityState.Modified;

        logger.LogInformation($"Total estimated route duration (including return): {route.TotalDuration} minutes");
        if (dayOffset > 0)
        {
            logger.LogInformation($"Route spans {dayOffset + 1} days");
        }
        logger.LogInformation("=== ESTIMATED ARRIVAL TIMES CALCULATED AND SAVED ===");
    }

    private async Task Optimize(Data.Journeys.Route route, string mode, bool avoidTolls, bool preserveOrder, CancellationToken cancellationToken)
    {
        await GoogleOptimizeAsync(route, mode, avoidTolls, preserveOrder, cancellationToken);
    }

    private async Task<External.Google.Models.DirectionsResponse> OptimizeAndCalculateETA(Data.Journeys.Route route, string mode, bool avoidTolls, bool preserveOrder, CancellationToken cancellationToken)
    {
        // Önce Google ile optimize et
        var directionsResponse = await GoogleOptimizeAsync(route, mode, avoidTolls, preserveOrder, cancellationToken);

        if (directionsResponse?.Routes?.FirstOrDefault()?.Legs != null)
        {
            logger.LogInformation("=== CALCULATING ETA FROM GOOGLE LEGS (NO ADDITIONAL API CALLS) ===");

            var startTime = route.StartDetails?.StartTime ?? new TimeSpan(8, 0, 0);
            var currentDepartureTime = startTime;
            var legs = directionsResponse.Routes.First().Legs;

            var orderedStops = route.Stops
                .Where(s => !s.IsExcluded)
                .OrderBy(x => x.Order)
                .ToList();

            logger.LogInformation($"Route start time: {startTime}, calculating ETA for {orderedStops.Count} stops from {legs.Count} legs");

            // Gece yarısı geçişini takip et
            int dayOffset = 0;

            for (int i = 0; i < orderedStops.Count && i < legs.Count; i++)
            {
                var stop = orderedStops[i];
                var leg = legs[i];

                if (leg.Duration != null)
                {
                    var travelTimeMinutes = (int)(leg.Duration.Value / 60);
                    var arrivalTime = currentDepartureTime.Add(TimeSpan.FromMinutes(travelTimeMinutes));
                    var serviceTime = stop.ServiceTime ?? route.Workspace?.DefaultServiceTime ?? TimeSpan.FromMinutes(10);
                    var departureTime = arrivalTime.Add(serviceTime);

                    // SQL TIME veri tipi için 24 saat sınırına uygun hale getir
                    TimeSpan sqlArrivalTime = arrivalTime;
                    TimeSpan sqlDepartureTime = departureTime;

                    if (sqlArrivalTime.TotalHours >= 24)
                    {
                        int days = (int)(sqlArrivalTime.TotalHours / 24);
                        sqlArrivalTime = sqlArrivalTime.Subtract(TimeSpan.FromDays(days));
                        dayOffset += days;
                    }

                    if (sqlDepartureTime.TotalHours >= 24)
                    {
                        int days = (int)(sqlDepartureTime.TotalHours / 24);
                        sqlDepartureTime = sqlDepartureTime.Subtract(TimeSpan.FromDays(days));
                    }

                    if (sqlArrivalTime < TimeSpan.Zero)
                        sqlArrivalTime = sqlArrivalTime.Add(TimeSpan.FromDays(1));
                    if (sqlDepartureTime < TimeSpan.Zero)
                        sqlDepartureTime = sqlDepartureTime.Add(TimeSpan.FromDays(1));

                    if (sqlArrivalTime >= TimeSpan.FromHours(24))
                        sqlArrivalTime = TimeSpan.FromHours(23).Add(TimeSpan.FromMinutes(59));
                    if (sqlDepartureTime >= TimeSpan.FromHours(24))
                        sqlDepartureTime = TimeSpan.FromHours(23).Add(TimeSpan.FromMinutes(59));

                    stop.EstimatedArrivalTime = sqlArrivalTime;
                    stop.EstimatedDepartureTime = sqlDepartureTime;

                    // Sadece veritabanında mevcut durakları Modified olarak işaretle
                    // Yeni eklenen duraklar (örn. depot final stop) zaten Added state'inde
                    if (stop.Id > 0)
                    {
                        context.Entry(stop).State = EntityState.Modified;
                    }

                    logger.LogInformation($"Stop {stop.Order} ({stop.Name}):");
                    logger.LogInformation($"  - Travel time from previous: {travelTimeMinutes} min (from Google leg)");
                    logger.LogInformation($"  - Service time: {serviceTime.TotalMinutes} min");
                    logger.LogInformation($"  - Actual arrival: {arrivalTime} (Day offset: {dayOffset})");
                    logger.LogInformation($"  - SQL arrival time: {sqlArrivalTime}");
                    logger.LogInformation($"  - SQL departure time: {sqlDepartureTime}");

                    currentDepartureTime = departureTime;
                }
            }

            // Son durağın EndDetails'ini hesapla (depoya dönüş)
            if (route.EndDetails != null && legs.Count > orderedStops.Count)
            {
                var finalLeg = legs[legs.Count - 1]; // Son leg = son durak → depot
                if (finalLeg.Duration != null)
                {
                    var returnTravelTime = (int)(finalLeg.Duration.Value / 60);
                    var depotArrivalTime = currentDepartureTime.Add(TimeSpan.FromMinutes(returnTravelTime));

                    TimeSpan sqlDepotArrivalTime = depotArrivalTime;
                    if (sqlDepotArrivalTime.TotalHours >= 24)
                    {
                        int days = (int)(sqlDepotArrivalTime.TotalHours / 24);
                        sqlDepotArrivalTime = sqlDepotArrivalTime.Subtract(TimeSpan.FromDays(days));
                    }
                    if (sqlDepotArrivalTime < TimeSpan.Zero)
                        sqlDepotArrivalTime = sqlDepotArrivalTime.Add(TimeSpan.FromDays(1));
                    if (sqlDepotArrivalTime >= TimeSpan.FromHours(24))
                        sqlDepotArrivalTime = TimeSpan.FromHours(23).Add(TimeSpan.FromMinutes(59));

                    route.EndDetails.EstimatedArrivalTime = sqlDepotArrivalTime;

                    // Sadece veritabanında mevcut EndDetails'i Modified olarak işaretle
                    if (route.EndDetails.Id > 0)
                    {
                        context.Entry(route.EndDetails).State = EntityState.Modified;
                    }

                    logger.LogInformation($"Depot return:");
                    logger.LogInformation($"  - Travel time from last stop: {returnTravelTime} min (from Google leg)");
                    logger.LogInformation($"  - Depot arrival time: {sqlDepotArrivalTime}");
                }
            }

            logger.LogInformation("=== ETA CALCULATED FROM GOOGLE LEGS (NO EXTRA API CALLS) ===");
        }

        return directionsResponse;
    }

    private async Task<External.Google.Models.DirectionsResponse> GoogleOptimizeAsync(Data.Journeys.Route route, string mode, bool avoidTolls, bool preserveOrder, CancellationToken cancellationToken)
    {
        logger.LogInformation("=== GOOGLE OPTIMIZE START ===");
        
        var origin = string.Format(CultureInfo.InvariantCulture, "{0},{1}", route.Depot.Latitude, route.Depot.Longitude);
        var destination = origin;

        // Sadece excluded olmayan stops'ları optimize et
        var originalStops = route.Stops
            .Where(s => !s.IsExcluded)
            .OrderBy(x => x.Order)
            .ToList();

        // Position constraints kontrolü
        var firstStops = originalStops.Where(s => s.OrderType == Data.Journeys.OrderType.First).ToList();
        var lastStops = originalStops.Where(s => s.OrderType == Data.Journeys.OrderType.Last).ToList();
        var autoStops = originalStops.Where(s => s.OrderType == Data.Journeys.OrderType.Auto).ToList();

        bool hasPositionConstraints = firstStops.Any() || lastStops.Any();

        // DEBUGGING: Position constraints detayları
        logger.LogInformation("=== POSITION CONSTRAINTS DEBUG ===");
        logger.LogInformation("First stops: {Count} - [{Names}]", firstStops.Count, string.Join(", ", firstStops.Select(s => s.Name)));
        logger.LogInformation("Last stops: {Count} - [{Names}]", lastStops.Count, string.Join(", ", lastStops.Select(s => s.Name)));
        logger.LogInformation("Auto stops: {Count} - [{Names}]", autoStops.Count, string.Join(", ", autoStops.Select(s => s.Name)));
        logger.LogInformation("Has position constraints: {HasConstraints}", hasPositionConstraints);

        List<Data.Journeys.RouteStop> orderedStops;
        bool shouldOptimize;

        if (hasPositionConstraints)
        {
            logger.LogInformation("Position constraints detected: {FirstCount} first stops, {LastCount} last stops",
                firstStops.Count, lastStops.Count);
            logger.LogInformation("Auto stops count: {AutoCount} (need >1 for optimization)", autoStops.Count);

            // Position constraint varken: First stop'tan başlayarak optimize et
            if (autoStops.Count > 1)
            {
                logger.LogInformation("Optimizing {AutoCount} auto stops with position constraints", autoStops.Count);

                // ✅ DÜZELTME: Auto stopları optimize et, ama origin'i first stop yap
                orderedStops = autoStops; // Sadece auto stopları optimize et
                shouldOptimize = !preserveOrder; // ✅ YENİ: PreserveOrder true ise optimize etme

                // Origin'i first stop olarak değiştir (depot yerine)
                if (firstStops.Any())
                {
                    var firstStop = firstStops.First();
                    origin = firstStop.Customer != null
                        ? string.Format(CultureInfo.InvariantCulture, "{0},{1}", firstStop.Customer.Latitude, firstStop.Customer.Longitude)
                        : string.Format(CultureInfo.InvariantCulture, "{0},{1}", firstStop.Latitude, firstStop.Longitude);
                    logger.LogInformation("Changed origin to first stop: {Name} at {Origin}", firstStop.Name, origin);
                }
            }
            else
            {
                // Auto stop yoksa veya 1 taneyse, optimize etmeye gerek yok
                orderedStops = autoStops;
                shouldOptimize = false;
            }
        }
        else
        {
            // Normal optimization - tüm stopları optimize et
            orderedStops = originalStops;
            shouldOptimize = !preserveOrder; // ✅ YENİ: PreserveOrder true ise optimize etme
        }

        var waypoints = orderedStops.Select(s =>
        {
            if (s.Customer != null)
                return string.Format(CultureInfo.InvariantCulture, "{0},{1}", s.Customer.Latitude, s.Customer.Longitude);
            return string.Format(CultureInfo.InvariantCulture, "{0},{1}", s.Latitude, s.Longitude);
        }).ToList();

        // ✅ Depot'u son waypoint olarak ekle (circular route optimization için)
        if (shouldOptimize) // Sadece optimize edilecekse depot ekle
        {
            var depotWaypoint = string.Format(CultureInfo.InvariantCulture, "{0},{1}", route.Depot.Latitude, route.Depot.Longitude);
            waypoints.Add(depotWaypoint);
            logger.LogInformation("Added depot as final waypoint for circular route optimization: waypoints count = {Count}", waypoints.Count);
        }

        var directionsResponse = await googleApiService.GetDirections(origin, destination, waypoints, shouldOptimize, avoidTolls);

        if (directionsResponse?.Routes?.FirstOrDefault() != null)
        {
            var optimizedRoute = directionsResponse.Routes.First();
            
            if (optimizedRoute.OverviewPolyline != null)
            {
                route.Polyline = optimizedRoute.OverviewPolyline.Points;
                logger.LogInformation($"Polyline saved to route, length: {route.Polyline?.Length ?? 0}");
            }
            
            double totalDistance = 0;
            int totalDuration = 0;

            // ✅ DÜZELTME: Position constraint varken depot → first stop mesafesi de eklenmeli
            if (hasPositionConstraints && firstStops.Any())
            {
                // Depot → First stop arası mesafeyi Google'dan al
                var firstStop = firstStops.First();
                var firstStopCoord = firstStop.Customer != null
                    ? $"{firstStop.Customer.Latitude},{firstStop.Customer.Longitude}"
                    : $"{firstStop.Latitude},{firstStop.Longitude}";

                logger.LogInformation("Calculating depot to first stop distance...");

                try
                {
                    var depotToFirstResponse = await googleApiService.GetDirections(
                        $"{route.Depot.Latitude},{route.Depot.Longitude}",
                        firstStopCoord,
                        new List<string>(),
                        false,
                        avoidTolls);

                    if (depotToFirstResponse?.Routes?.FirstOrDefault()?.Legs?.FirstOrDefault() != null)
                    {
                        var depotToFirstLeg = depotToFirstResponse.Routes.First().Legs.First();
                        if (depotToFirstLeg.Distance != null && depotToFirstLeg.Duration != null)
                        {
                            totalDistance += depotToFirstLeg.Distance.Value / 1000.0;
                            totalDuration += (int)(depotToFirstLeg.Duration.Value / 60);
                            logger.LogInformation("Added depot → first stop: {Distance}km, {Duration}min",
                                depotToFirstLeg.Distance.Value / 1000.0, depotToFirstLeg.Duration.Value / 60);
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to get depot to first stop distance, using estimate");
                    // Fallback: yaklaşık mesafe hesapla
                    totalDistance += 5; // 5km varsayım
                    totalDuration += 10; // 10dk varsayım
                }
            }

            // Ana optimizasyon rotasının mesafe/süresi
            foreach (var leg in optimizedRoute.Legs)
            {
                if (leg.Distance != null && leg.Duration != null)
                {
                    totalDistance += leg.Distance.Value / 1000.0;
                    totalDuration += (int)(leg.Duration.Value / 60);
                }
            }
            
            foreach (var stop in originalStops)
            {
                int serviceMinutes = 10;
                if (stop.ServiceTime.HasValue) serviceMinutes = (int)stop.ServiceTime.Value.TotalMinutes;
                else if (route.Workspace != null) serviceMinutes = (int)route.Workspace.DefaultServiceTime.TotalMinutes;
                totalDuration += serviceMinutes;
            }
            
            route.TotalDistance = totalDistance;
            route.TotalDuration = totalDuration;
            route.Optimized = true;

            // AvoidTolls ayarını sakla
            route.AvoidTolls = avoidTolls;
            
            if (hasPositionConstraints)
            {
                // Position constraint varken, optimize edilmiş auto stopları first/last arasına yerleştir
                var finalOrderedStops = new List<Data.Journeys.RouteStop>();

                // 1. First stopları başa ekle
                finalOrderedStops.AddRange(firstStops);

                // 2. Optimize edilmiş auto stopları ortaya ekle
                if (shouldOptimize && directionsResponse.WaypointOrder != null && directionsResponse.WaypointOrder.Any())
                {
                    // Google Maps optimized order'ı kullan
                    logger.LogInformation("Using Google Maps optimized order for auto stops");
                    logger.LogInformation("Original waypoint order: [{Order}]", string.Join(", ", directionsResponse.WaypointOrder));

                    // ✅ DÜZELTME: Depot waypoint dahil olduğu için filtrelemeyi güncelle
                    // autoStops + depot = autoStops.Count + 1 waypoint var
                    var stopIndices = directionsResponse.WaypointOrder.ToList();
                    logger.LogInformation("All waypoint indices from Google: [{Order}]", string.Join(", ", stopIndices));

                    logger.LogInformation("Filtered stop indices: [{Indices}]", string.Join(", ", stopIndices));

                    var googleOptimizedStops = new List<Data.Journeys.RouteStop>();
                    foreach (var originalIndex in stopIndices)
                    {
                        // Son index depot'u gösterir - onu atla
                        if (originalIndex < autoStops.Count)
                        {
                            googleOptimizedStops.Add(autoStops[originalIndex]);
                            logger.LogInformation("Added optimized auto stop: {Name} (index {Index})", autoStops[originalIndex].Name, originalIndex);
                        }
                        else if (originalIndex == autoStops.Count)
                        {
                            // Depot index'i - şimdilik kaydet ama son sıraya database'e eklenir
                            logger.LogInformation("Found depot in optimization order at index {Index} - will be added as final stop to database", originalIndex);
                        }
                    }

                    // Depoyu son durak olarak database'e ekle (Google optimize ettikten sonra)
                    logger.LogInformation("Depot will be added as final stop after optimization");

                    // 2-Opt improvement - SADECE position constraint yoksa uygula
                    if (!hasPositionConstraints)
                    {
                        logger.LogInformation("Applying 2-Opt improvement for circular route optimization");
                        var circularOptimizedStops = Apply2OptImprovement(googleOptimizedStops, route.Depot);
                        finalOrderedStops.AddRange(circularOptimizedStops);
                    }
                    else
                    {
                        logger.LogInformation("Skipping 2-Opt improvement due to position constraints (first stop must remain first)");
                        finalOrderedStops.AddRange(googleOptimizedStops);
                    }
                }
                else
                {
                    // Optimize edilmemişse auto stopları mevcut sıralarıyla ekle
                    finalOrderedStops.AddRange(autoStops);

                    logger.LogInformation("Depot will be added as final stop (non-optimized route)");
                }

                // 3. Last stopları sona ekle (eğer varsa)
                finalOrderedStops.AddRange(lastStops);

                // Depoyu son durak olarak database'e ekle
                var depotStop = new Data.Journeys.RouteStop();
                context.Entry(depotStop).State = EntityState.Added;
                context.Entry(depotStop).CurrentValues.SetValues(new
                {
                    Name = route.Depot.Name + " (Dönüş)",
                    Address = route.Depot.Address,
                    Latitude = route.Depot.Latitude,
                    Longitude = route.Depot.Longitude,
                    OrderType = OrderType.Last,
                    Type = 1, // Delivery
                    ServiceTime = TimeSpan.FromMinutes(5),
                    RouteId = route.Id,
                    CustomerId = (int?)null, // Depot için customer yok
                    ContactFullName = route.Depot.Name,
                    ContactPhone = "",
                    ContactEmail = "",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    Order = finalOrderedStops.Count + 1,
                    EstimatedArrivalTime = (TimeSpan?)null,
                    EstimatedDepartureTime = (TimeSpan?)null,
                    ArriveBetweenStart = (TimeSpan?)null,
                    ArriveBetweenEnd = (TimeSpan?)null,
                    ExclusionReason = "",
                    IsDeleted = false,
                    IsExcluded = false,
                    Notes = "",
                    PhotoRequired = false,
                    ProofOfDeliveryRequired = false,
                    SignatureRequired = false
                });
                finalOrderedStops.Add(depotStop);
                logger.LogInformation("Added depot as final stop: {DepotName}", route.Depot.Name);

                // Final sıralamayı database'e kaydet
                for (int newOrder = 0; newOrder < finalOrderedStops.Count - 1; newOrder++) // -1 çünkü depot stop zaten eklendi
                {
                    var stop = finalOrderedStops[newOrder];
                    stop.Order = newOrder + 1;

                    // Time window değerlerini korumak için explicitly mark edelim
                    context.Entry(stop).State = EntityState.Modified;
                    context.Entry(stop).Property(x => x.ArriveBetweenStart).IsModified = true;
                    context.Entry(stop).Property(x => x.ArriveBetweenEnd).IsModified = true;

                    logger.LogInformation("Position constraint final ordering: {Name} -> Order {Order} (Type: {OrderType}, TimeWindow: {Start}-{End})",
                        stop.Name, newOrder + 1, stop.OrderType, stop.ArriveBetweenStart, stop.ArriveBetweenEnd);
                }
            }
            else if (directionsResponse.WaypointOrder != null && directionsResponse.WaypointOrder.Any())
            {
                // Normal Google optimization
                logger.LogInformation("Normal optimization: Original waypoint order: [{Order}]", string.Join(", ", directionsResponse.WaypointOrder));

                // ✅ DÜZELTME: Depot dahil edildi, tüm waypoint'leri işle
                var stopIndices = directionsResponse.WaypointOrder.ToList();

                logger.LogInformation("Normal optimization: Filtered stop indices: [{Indices}]", string.Join(", ", stopIndices));
                logger.LogInformation("Normal optimization: processing {Count} stops", stopIndices.Count);

                int newOrder = 0;
                var finalOrderedStops = new List<Data.Journeys.RouteStop>();

                foreach (var originalIndex in stopIndices)
                {
                    // Son index depot'u gösterir - onu atla, database'e ayrıca ekleyeceğiz
                    if (originalIndex < originalStops.Count)
                    {
                        var stop = originalStops[originalIndex];
                        stop.Order = newOrder + 1;

                        // Time window değerlerini korumak için explicitly mark edelim
                        context.Entry(stop).State = EntityState.Modified;
                        context.Entry(stop).Property(x => x.ArriveBetweenStart).IsModified = true;
                        context.Entry(stop).Property(x => x.ArriveBetweenEnd).IsModified = true;

                        logger.LogInformation("Normal optimization: {Name} -> Order {Order} (TimeWindow: {Start}-{End})",
                            stop.Name, newOrder + 1, stop.ArriveBetweenStart, stop.ArriveBetweenEnd);
                        finalOrderedStops.Add(stop);
                        newOrder++;
                    }
                    else if (originalIndex == originalStops.Count)
                    {
                        // Depot index'i - son sıraya database'e eklenecek
                        logger.LogInformation("Found depot in optimization order at index {Index} - will be added as final stop to database", originalIndex);
                    }
                }

                // Depoyu son durak olarak database'e ekle (Normal optimization için de) - Sadece yoksa
                var existingDepotStop = route.Stops.FirstOrDefault(s =>
                    s.CustomerId == null &&
                    s.Latitude == route.Depot.Latitude &&
                    s.Longitude == route.Depot.Longitude &&
                    s.OrderType == OrderType.Last &&
                    !s.IsExcluded);

                if (existingDepotStop == null)
                {
                    var depotStop = new Data.Journeys.RouteStop();
                    context.Entry(depotStop).State = EntityState.Added;
                    context.Entry(depotStop).CurrentValues.SetValues(new
                    {
                        Name = route.Depot.Name + " (Dönüş)",
                        Address = route.Depot.Address,
                        Latitude = route.Depot.Latitude,
                        Longitude = route.Depot.Longitude,
                        OrderType = OrderType.Last,
                        Type = 1, // Delivery
                        ServiceTime = TimeSpan.FromMinutes(5),
                        RouteId = route.Id,
                        CustomerId = (int?)null, // Depot için customer yok
                        ContactFullName = route.Depot.Name,
                        ContactPhone = "",
                        ContactEmail = "",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        Order = finalOrderedStops.Count + 1,
                        EstimatedArrivalTime = (TimeSpan?)null,
                        EstimatedDepartureTime = (TimeSpan?)null,
                        ArriveBetweenStart = (TimeSpan?)null,
                        ArriveBetweenEnd = (TimeSpan?)null,
                        ExclusionReason = "",
                        IsDeleted = false,
                        IsExcluded = false,
                        Notes = "",
                        PhotoRequired = false,
                        ProofOfDeliveryRequired = false,
                        SignatureRequired = false
                    });
                    logger.LogInformation("Added depot as final stop for normal optimization: {DepotName}", route.Depot.Name);
                }
                else
                {
                    // Var olan depot stop'un order'ını güncelle
                    existingDepotStop.Order = finalOrderedStops.Count + 1;
                    context.Entry(existingDepotStop).State = EntityState.Modified;
                    logger.LogInformation("Updated existing depot stop order to: {Order}", finalOrderedStops.Count + 1);
                }
            }
            
            context.Routes.Update(route);
        }
        else
        {
            logger.LogError("Google Maps API returned no results!");
            throw new ApiException("Could not optimize route. Google Maps API returned no results.", 500);
        }

        logger.LogInformation("=== GOOGLE OPTIMIZE END ===");
        return directionsResponse;
    }

    private List<Data.Journeys.RouteStop> Apply2OptImprovement(List<Data.Journeys.RouteStop> stops, Data.Workspace.Depot depot)
    {
        if (stops.Count < 4) return stops; // 2-Opt en az 4 durak gerektirir

        var improved = stops.ToList();
        var depotLat = depot.Latitude;
        var depotLng = depot.Longitude;

        const int maxIterations = 500; // Daha fazla iterasyon
        const double minImprovementThreshold = 0.001; // 1 metre minimum iyileştirme

        logger.LogInformation("Starting enhanced 2-Opt improvement with {Count} stops", stops.Count);

        // Phase 1: Aggressive 2-Opt with multiple passes
        improved = Apply2OptPhase(improved, depot, maxIterations, "Aggressive", 1.0);

        // Phase 2: Fine-tuning with stricter criteria
        improved = Apply2OptPhase(improved, depot, maxIterations / 2, "Fine-tuning", minImprovementThreshold);

        // Phase 3: Multiple random restart attempts for better local optima escape
        for (int restart = 0; restart < 3; restart++)
        {
            var shuffled = CreateRandomizedVariant(improved, depot, restart + 1);
            var optimized = Apply2OptPhase(shuffled, depot, maxIterations / 4, $"Restart-{restart + 1}", minImprovementThreshold);

            if (CalculateTotalDistance(optimized, depot) < CalculateTotalDistance(improved, depot))
            {
                improved = optimized;
                logger.LogInformation("Random restart {Restart} found better solution", restart + 1);
            }
        }

        logger.LogInformation("Enhanced 2-Opt improvement completed");
        return improved;
    }

    private List<Data.Journeys.RouteStop> Apply2OptPhase(List<Data.Journeys.RouteStop> stops, Data.Workspace.Depot depot, int maxIterations, string phaseName, double improvementThreshold)
    {
        var improved = stops.ToList();
        var depotLat = depot.Latitude;
        var depotLng = depot.Longitude;
        bool hasImprovement;
        int iterations = 0;
        double totalImprovement = 0;

        logger.LogInformation("Starting {Phase} phase with {Count} stops", phaseName, stops.Count);

        do
        {
            hasImprovement = false;
            iterations++;

            // Try different scanning orders for better exploration
            var scanOrder = (iterations % 3) switch
            {
                0 => Enumerable.Range(0, improved.Count - 1).ToList(), // Forward
                1 => Enumerable.Range(0, improved.Count - 1).Reverse().ToList(), // Backward
                _ => Enumerable.Range(0, improved.Count - 1).OrderBy(x => new Random(iterations).Next()).ToList() // Random
            };

            foreach (int i in scanOrder)
            {
                for (int j = i + 2; j < improved.Count; j++)
                {
                    if (j == improved.Count - 1 && i == 0) continue; // Son ve ilk durak birbirine bağlı, swap yapma

                    // Mevcut bağlantılar
                    var currentDistance = CalculateDistance(
                        i == 0 ? depotLat : improved[i - 1].Customer?.Latitude ?? improved[i - 1].Latitude,
                        i == 0 ? depotLng : improved[i - 1].Customer?.Longitude ?? improved[i - 1].Longitude,
                        improved[i].Customer?.Latitude ?? improved[i].Latitude,
                        improved[i].Customer?.Longitude ?? improved[i].Longitude)
                    + CalculateDistance(
                        improved[j].Customer?.Latitude ?? improved[j].Latitude,
                        improved[j].Customer?.Longitude ?? improved[j].Longitude,
                        j == improved.Count - 1 ? depotLat : improved[j + 1].Customer?.Latitude ?? improved[j + 1].Latitude,
                        j == improved.Count - 1 ? depotLng : improved[j + 1].Customer?.Longitude ?? improved[j + 1].Longitude);

                    // Yeni bağlantılar (2-opt swap)
                    var newDistance = CalculateDistance(
                        i == 0 ? depotLat : improved[i - 1].Customer?.Latitude ?? improved[i - 1].Latitude,
                        i == 0 ? depotLng : improved[i - 1].Customer?.Longitude ?? improved[i - 1].Longitude,
                        improved[j].Customer?.Latitude ?? improved[j].Latitude,
                        improved[j].Customer?.Longitude ?? improved[j].Longitude)
                    + CalculateDistance(
                        improved[i].Customer?.Latitude ?? improved[i].Latitude,
                        improved[i].Customer?.Longitude ?? improved[i].Longitude,
                        j == improved.Count - 1 ? depotLat : improved[j + 1].Customer?.Latitude ?? improved[j + 1].Latitude,
                        j == improved.Count - 1 ? depotLng : improved[j + 1].Customer?.Longitude ?? improved[j + 1].Longitude);

                    var improvement = currentDistance - newDistance;

                    // Improvement varsa ve threshold'u geçiyorsa swap yap
                    if (improvement > improvementThreshold)
                    {
                        // i ile j arasındaki segment'i reverse et
                        var segment = improved.GetRange(i, j - i + 1);
                        segment.Reverse();
                        improved.RemoveRange(i, j - i + 1);
                        improved.InsertRange(i, segment);
                        hasImprovement = true;
                        totalImprovement += improvement;
                        logger.LogInformation("{Phase} 2-Opt improvement: swapped segment [{Start}-{End}], saved {Distance:F3} km",
                            phaseName, i, j, improvement / 1000.0);
                        break; // Değişiklik yapıldı, yeniden başla
                    }
                }
                if (hasImprovement) break; // Outer loop'tan da çık
            }
        } while (hasImprovement && iterations < maxIterations);

        logger.LogInformation("{Phase} completed after {Iterations} iterations, total improvement: {TotalImprovement:F3} km",
            phaseName, iterations, totalImprovement / 1000.0);
        return improved;
    }

    private List<Data.Journeys.RouteStop> CreateRandomizedVariant(List<Data.Journeys.RouteStop> stops, Data.Workspace.Depot depot, int seed)
    {
        var result = stops.ToList();
        var random = new Random(seed * 42); // Deterministic random for consistency

        // Apply 2-3 random swaps to escape local optima
        for (int swap = 0; swap < Math.Min(3, result.Count / 4); swap++)
        {
            int i = random.Next(0, result.Count - 2);
            int j = random.Next(i + 2, result.Count);

            // Preserve position constraints - don't move first or last stops
            if (result[i].OrderType == OrderType.First || result[i].OrderType == OrderType.Last ||
                (j < result.Count && (result[j].OrderType == OrderType.First || result[j].OrderType == OrderType.Last))) continue;

            // Swap segments
            var segment = result.GetRange(i, j - i + 1);
            segment.Reverse();
            result.RemoveRange(i, j - i + 1);
            result.InsertRange(i, segment);
        }

        return result;
    }

    private double CalculateTotalDistance(List<Data.Journeys.RouteStop> stops, Data.Workspace.Depot depot)
    {
        if (!stops.Any()) return 0;

        double total = 0;
        var depotLat = depot.Latitude;
        var depotLng = depot.Longitude;

        // Depot to first stop
        total += CalculateDistance(depotLat, depotLng,
            stops.First().Customer?.Latitude ?? stops.First().Latitude,
            stops.First().Customer?.Longitude ?? stops.First().Longitude);

        // Between stops
        for (int i = 0; i < stops.Count - 1; i++)
        {
            total += CalculateDistance(
                stops[i].Customer?.Latitude ?? stops[i].Latitude,
                stops[i].Customer?.Longitude ?? stops[i].Longitude,
                stops[i + 1].Customer?.Latitude ?? stops[i + 1].Latitude,
                stops[i + 1].Customer?.Longitude ?? stops[i + 1].Longitude);
        }

        // Last stop to depot
        total += CalculateDistance(
            stops.Last().Customer?.Latitude ?? stops.Last().Latitude,
            stops.Last().Customer?.Longitude ?? stops.Last().Longitude,
            depotLat, depotLng);

        return total;
    }

    private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        // Haversine formula with 1.4x road factor
        const double R = 6371000; // Earth radius in meters
        var dLat = (lat2 - lat1) * Math.PI / 180;
        var dLon = (lon2 - lon1) * Math.PI / 180;
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c * 1.4; // 1.4x road factor for realistic distance
    }

    private static bool IsInvalidCoordinate(double latitude, double longitude)
    {
        return latitude == 0 || longitude == 0 || double.IsNaN(latitude) || double.IsNaN(longitude);
    }
}
