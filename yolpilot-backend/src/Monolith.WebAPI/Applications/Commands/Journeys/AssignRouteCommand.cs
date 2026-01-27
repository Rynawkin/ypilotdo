using System.Text.Json.Serialization;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Data.Journeys;
using Monolith.WebAPI.Data.Notifications;
using Monolith.WebAPI.Infrastructure;
using Monolith.WebAPI.Responses.Journeys;
using Monolith.WebAPI.Services.Members;
using Monolith.WebAPI.Services.Email;
using Monolith.WebAPI.Services.Notifications;
using Monolith.WebAPI.Services.Subscription;
using Route = Monolith.WebAPI.Data.Journeys.Route;

namespace Monolith.WebAPI.Applications.Commands.Journeys;

public class AssignRouteCommand : BaseAuthenticatedCommand<JourneyResponse>
{
    [JsonIgnore] public override bool RequiresDispatcher => true;
    [JsonIgnore] public override bool RequiresWorkspaceAccess => true;
    public int RouteId { get; set; }
    public int DriverId { get; set; }
    public string? Name { get; set; } // ✅ YENİ EKLENEN - Sefer ismi
    public int StartKm { get; set; } // ✅ YENİ - Başlangıç kilometresi (ZORUNLU)

    public Journey ToEntity(Route route)
    {
        var journey = new Journey(this, route);

        // Name varsa Journey'ye ekle (Route adından farklı bir isim verilebilir)
        if (!string.IsNullOrEmpty(Name))
        {
            journey.Name = Name;
        }
        else
        {
            journey.Name = route.Name; // Fallback olarak route adı
        }

        // StartKm'yi set et
        journey.StartKm = StartKm;

        return journey;
    }
}

public class AssignRouteCommandValidator : AbstractValidator<AssignRouteCommand>
{
    public AssignRouteCommandValidator()
    {
        RuleFor(x => x.RouteId).NotEmpty();
        RuleFor(x => x.DriverId).NotEmpty();
        RuleFor(x => x.Name)
            .MaximumLength(200).WithMessage("Sefer adı en fazla 200 karakter olabilir")
            .When(x => !string.IsNullOrWhiteSpace(x.Name)); // ✅ YENİ EKLENEN - Name validasyonu

        // ✅ YENİ - Kilometre validasyonu
        RuleFor(x => x.StartKm)
            .GreaterThan(0).WithMessage("Başlangıç kilometresi 0'dan büyük olmalıdır");
    }
}

public class AssignRouteCommandHandler : BaseAuthenticatedCommandHandler<AssignRouteCommand, JourneyResponse>
{
    private readonly AppDbContext _context;
    private readonly IMediator _mediator;
    private readonly IEmailService _emailService;
    private readonly INotificationService _notificationService;
    private readonly ISubscriptionService _subscriptionService;
    private readonly ILogger<AssignRouteCommandHandler> _logger;

    public AssignRouteCommandHandler(
        AppDbContext context, 
        IUserService userService,
        IMediator mediator,
        IEmailService emailService,
        INotificationService notificationService,
        ISubscriptionService subscriptionService,
        ILogger<AssignRouteCommandHandler> logger)
        : base(userService)
    {
        _context = context;
        _mediator = mediator;
        _emailService = emailService;
        _notificationService = notificationService;
        _subscriptionService = subscriptionService;
        _logger = logger;
    }

    protected override async Task<JourneyResponse> HandleCommand(AssignRouteCommand request, CancellationToken cancellationToken)
    {
        // Route'u tüm ilişkili verilerle birlikte al
        var route = await _context.Routes
            .Include(x => x.Workspace)
            .Include(x => x.Depot)
            .Include(x => x.StartDetails)
            .Include(x => x.EndDetails)
            .Include(x => x.Stops)
                .ThenInclude(s => s.Customer)
            .Include(x => x.Driver)
            .Include(x => x.Vehicle)
            .FirstOrDefaultAsync(x => x.Id == request.RouteId && x.WorkspaceId == User.WorkspaceId, cancellationToken);
            
        if (route is null)
            throw new ApiException("Route not found.", 404);

        // Driver kontrolü - User bilgisini de al
        var driver = await _context.Set<Data.Workspace.Driver>()
            .Include(d => d.User)
            .FirstOrDefaultAsync(d => d.Id == request.DriverId && d.WorkspaceId == User.WorkspaceId, cancellationToken);
        
        if (driver is null)
            throw new ApiException("Driver not found.", 404);

        // ✅ YENİ - Kilometre kontrolü: StartKm aracın mevcut kilometresinden küçük olamaz
        if (route.VehicleId.HasValue)
        {
            var vehicle = await _context.Vehicles
                .FirstOrDefaultAsync(v => v.Id == route.VehicleId.Value && v.WorkspaceId == User.WorkspaceId, cancellationToken);

            if (vehicle != null && vehicle.CurrentKm.HasValue)
            {
                if (request.StartKm < vehicle.CurrentKm.Value)
                {
                    throw new ApiException(
                        $"Başlangıç kilometresi ({request.StartKm:N0} km) aracın mevcut kilometresinden ({vehicle.CurrentKm.Value:N0} km) küçük olamaz.",
                        400);
                }
            }
        }

        // ✅ Trial kullanıcıları için ÖNCE limit kontrolü yap
        int expectedStopCount = 0;
        if (route.Optimized && route.Stops?.Count > 0)
        {
            expectedStopCount = route.Stops.Count(s => !s.IsExcluded);
        }
        else if (route.Stops?.Count > 0)
        {
            expectedStopCount = route.Stops.Count(s => !s.IsExcluded);
        }

        if (expectedStopCount > 0)
        {
            // Trial kullanıcıları için limit kontrolü - Journey oluşturmadan ÖNCE
            var canAddStop = await _subscriptionService.CanAddStop(User.WorkspaceId);
            if (!canAddStop)
            {
                throw new ApiException("Trial limitinize ulaştınız. Devam etmek için plan yükseltin.", 403);
            }
        }

        // ✅ YENİ EKLENEN - Eğer özel isim varsa Journey'ye ata
        // NOT: Journey entity'sine Name property'si eklenmeli veya mevcut bir property kullanılmalı
        var journey = new Journey(request, route);

        // Name'i set et
        if (!string.IsNullOrEmpty(request.Name))
        {
            journey.Name = request.Name;
            _logger.LogInformation($"Creating journey with custom name: {request.Name}");
        }
        else
        {
            journey.Name = route.Name;
            _logger.LogInformation($"Using route name as journey name: {route.Name}");
        }
        
        // Optimizasyon kodu aynı kalacak...
        if (route.Optimized && route.Stops?.Count > 0)
        {
            _logger.LogInformation($"Route already optimized with {route.Stops.Count} stops. Creating journey stops directly...");
            
            journey.Stops = new List<JourneyStop>();
            
            // Excluded olmayan stops'ları Order'a göre sırala
            var orderedStops = route.Stops
                .Where(s => !s.IsExcluded)
                .OrderBy(s => s.Order)
                .ToList();
                
            var journeyOrder = 1;
            TimeSpan currentTime = route.StartDetails?.StartTime ?? TimeSpan.FromHours(9);
            
            _logger.LogInformation($"Creating journey stops from {orderedStops.Count} non-excluded route stops");
            
            foreach (var routeStop in orderedStops)
            {
                var journeyStop = new JourneyStop
                {
                    JourneyId = journey.Id,
                    StopId = routeStop.Id,
                    RouteStopId = routeStop.Id,
                    Order = journeyOrder++,
                    Status = JourneyStopStatus.Pending,
                    Distance = 0,
                    StartAddress = routeStop.Address ?? "",
                    StartLatitude = routeStop.Latitude,
                    StartLongitude = routeStop.Longitude,
                    EndAddress = routeStop.Address ?? "",
                    EndLatitude = routeStop.Latitude,
                    EndLongitude = routeStop.Longitude,
                    EstimatedArrivalTime = routeStop.EstimatedArrivalTime ?? currentTime,
                    EstimatedDepartureTime = routeStop.EstimatedDepartureTime ?? 
                        (routeStop.EstimatedArrivalTime ?? currentTime).Add(routeStop.ServiceTime ?? route.Workspace.DefaultServiceTime),
                    ArriveBetweenStart = routeStop.ArriveBetweenStart,
                    ArriveBetweenEnd = routeStop.ArriveBetweenEnd
                };
                
                journey.Stops.Add(journeyStop);

                // Bir sonraki durak için currentTime güncelle
                if (routeStop.EstimatedDepartureTime.HasValue)
                {
                    currentTime = routeStop.EstimatedDepartureTime.Value;
                }
                else if (journeyStop.EstimatedDepartureTime.HasValue)
                {
                    currentTime = journeyStop.EstimatedDepartureTime.Value;
                }
            }
            
            // Son durağın departure time'ını null yap
            if (journey.Stops.Count > 0)
            {
                journey.Stops.Last().EstimatedDepartureTime = null;
            }
            
            _logger.LogInformation($"Created {journey.Stops.Count} journey stops from route stops.");
        }
        
        _logger.LogInformation($"[DEBUG-BEFORE-ADD] Journey before adding to context - Status: {journey.Status}, StartedAt: {journey.StartedAt}");

        await _context.Journeys.AddAsync(journey, cancellationToken);

        _logger.LogInformation($"[DEBUG-AFTER-ADD] Journey after AddAsync - Status: {journey.Status}, StartedAt: {journey.StartedAt}");

        // ✅ YENİ - Journey oluşturulduğunda aracın kilometresini güncelle
        if (route.VehicleId.HasValue)
        {
            var vehicle = await _context.Vehicles
                .FirstOrDefaultAsync(v => v.Id == route.VehicleId.Value && v.WorkspaceId == User.WorkspaceId, cancellationToken);

            if (vehicle != null)
            {
                vehicle.CurrentKm = request.StartKm;
                _logger.LogInformation($"Updated vehicle {vehicle.Id} CurrentKm to {request.StartKm} from journey creation");
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation($"[DEBUG-AFTER-SAVE] Journey after SaveChanges - Status: {journey.Status}, StartedAt: {journey.StartedAt}");

        // Subscription kullanımını kaydet (excluded olmayan duraklar için)
        if (journey.Stops != null && journey.Stops.Any())
        {
            var activeStopCount = journey.Stops.Count;
            
            if (activeStopCount > 0)
            {
                // Trial kullanıcıları için limit kontrolü
                var canAddStop = await _subscriptionService.CanAddStop(User.WorkspaceId);
                if (!canAddStop)
                {
                    // Journey'i veritabanından sil (henüz commit edilmedi)
                    _context.Journeys.Remove(journey);
                    throw new ApiException("Trial limitinize ulaştınız. Devam etmek için plan yükseltin.", 403);
                }

                await _subscriptionService.RecordStopUsage(User.WorkspaceId, activeStopCount);
                journey.MarkStopsAsCounted();
                await _context.SaveChangesAsync(cancellationToken);
                
                _logger.LogInformation($"Recorded {activeStopCount} stops for billing (Journey #{journey.Id}, Workspace #{User.WorkspaceId})");
            }
        }
        else if (route.Stops != null && route.Stops.Any())
        {
            // Journey stops henüz oluşturulmadıysa route stops'tan say
            var activeStopCount = route.Stops.Count(s => !s.IsExcluded);
            
            if (activeStopCount > 0)
            {
                // Trial kullanıcıları için limit kontrolü
                var canAddStop = await _subscriptionService.CanAddStop(User.WorkspaceId);
                if (!canAddStop)
                {
                    // Journey'i veritabanından sil (henüz commit edilmedi)
                    _context.Journeys.Remove(journey);
                    throw new ApiException("Trial limitinize ulaştınız. Devam etmek için plan yükseltin.", 403);
                }

                await _subscriptionService.RecordStopUsage(User.WorkspaceId, activeStopCount);
                journey.MarkStopsAsCounted();
                await _context.SaveChangesAsync(cancellationToken);
                
                _logger.LogInformation($"Recorded {activeStopCount} route stops for billing (Journey #{journey.Id}, Workspace #{User.WorkspaceId})");
            }
        }

        // Optimizasyon gerekiyorsa yap
        if (!route.Optimized || journey.Stops?.Count == 0)
        {
            try
            {
                _logger.LogInformation($"Route not optimized or no journey stops. Calling optimize command...");
                
                var optimizeCommand = new OptimizeJourneyCommand
                {
                    JourneyId = journey.Id,
                    AuthenticatedUserId = request.AuthenticatedUserId
                };
                
                var optimizedJourney = await _mediator.Send(optimizeCommand, cancellationToken);
                journey = await _context.Journeys
                    .Include(x => x.Route)
                        .ThenInclude(r => r.Stops)
                    .Include(x => x.Stops)
                    .FirstOrDefaultAsync(x => x.Id == journey.Id, cancellationToken);
                    
                _logger.LogInformation($"[DEBUG-AFTER-OPTIMIZE] Journey after optimize - Status: {journey.Status}, StartedAt: {journey.StartedAt}");
                
                // Optimize sonrası durak sayısı değişmiş olabilir, tekrar kontrol et
                if (!journey.StopsCountedForBilling && journey.Stops != null && journey.Stops.Any())
                {
                    var newStopCount = journey.Stops.Count;
                    
                    // Trial kullanıcıları için limit kontrolü
                    var canAddStop = await _subscriptionService.CanAddStop(User.WorkspaceId);
                    if (!canAddStop)
                    {
                        // Journey'i veritabanından sil
                        _context.Journeys.Remove(journey);
                        throw new ApiException("Trial limitinize ulaştınız. Devam etmek için plan yükseltin.", 403);
                    }
                    
                    await _subscriptionService.RecordStopUsage(User.WorkspaceId, newStopCount);
                    journey.MarkStopsAsCounted();
                    await _context.SaveChangesAsync(cancellationToken);
                    
                    _logger.LogInformation($"Recorded {newStopCount} stops after optimization (Journey #{journey.Id})");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Journey optimization failed");
            }
        }
        
        // Driver'a email ve notification gönder
        _logger.LogInformation($"Attempting to send notifications. Driver: {driver.Name}, Driver.User: {driver.User?.Id}, Driver.User.Email: {driver.User?.Email}");
        
        if (driver.User != null)
        {
            var journeyName = !string.IsNullOrWhiteSpace(request.Name) 
                ? request.Name 
                : route.Name;

            _logger.LogInformation($"Driver User found. Sending notifications for journey: {journeyName}, Driver ID: {driver.User.Id}");

            // Email gönder
            if (!string.IsNullOrEmpty(driver.User.Email))
            {
                try
                {
                    await _emailService.SendJourneyAssignmentEmail(
                        driver.User.Email,
                        driver.Name,
                        journey.Id,
                        journey.Date,
                        journey.Stops?.Count ?? route.Stops?.Where(s => !s.IsExcluded).Count() ?? 0
                    );
                    _logger.LogInformation($"Journey assignment email sent to driver {driver.Name} ({driver.User.Email}) for journey: {journeyName}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send journey assignment email");
                }
            }

            // Notification gönder
            try
            {
                _logger.LogInformation($"About to call CreateJourneyAssignedNotificationAsync with UserId: {driver.User.Id}, JourneyId: {journey.Id}, JourneyName: {journeyName}");
                
                await _notificationService.CreateJourneyAssignedNotificationAsync(
                    User.WorkspaceId,
                    driver.User.Id,
                    journey.Id,
                    journeyName
                );
                _logger.LogInformation($"Journey assignment notification sent to driver {driver.Name} for journey: {journeyName}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send journey assignment notification");
            }
        }
        else
        {
            _logger.LogWarning($"Driver.User is null for driver {driver.Name} (ID: {driver.Id}). Cannot send notifications.");
        }

        // Admin/Dispatcher'lara da bildir - TÜM Journey olayları
        try
        {
            var journeyName = !string.IsNullOrWhiteSpace(request.Name) ? request.Name : route.Name;
            await _notificationService.NotifyAdminDispatchersAsync(
                User.WorkspaceId,
                "Yeni Görev Atandı",
                $"{driver.Name} sürücüsüne {journeyName} rotası atandı.",
                NotificationType.JOURNEY_ASSIGNED,
                new { journeyId = journey.Id, driverName = driver.Name, routeName = journeyName }
            );
            _logger.LogInformation($"Journey assignment notification sent to admin/dispatchers for journey: {journeyName}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send journey assignment notification to admin/dispatchers");
        }
        
        // Journey'i tekrar yükle
        journey = await _context.Journeys
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
            .FirstOrDefaultAsync(x => x.Id == journey.Id, cancellationToken);
        
        _logger.LogInformation($"[DEBUG-FINAL] Journey final state - Status: {journey.Status}, StartedAt: {journey.StartedAt}");
        
        return new JourneyResponse(journey);
    }
}