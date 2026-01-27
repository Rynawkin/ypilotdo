using System.Text.Json.Serialization;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Data.Journeys;
using Monolith.WebAPI.Hubs;
using Monolith.WebAPI.Infrastructure;
using Monolith.WebAPI.Responses.Journeys;
using Monolith.WebAPI.Services.Members;
using Monolith.WebAPI.Services.Subscription;

namespace Monolith.WebAPI.Applications.Commands.Journeys;

public class AddStopToActiveJourneyCommand : BaseAuthenticatedCommand<JourneyResponse>
{
    [JsonIgnore] public override bool RequiresDispatcher => true; // Sadece dispatcher/admin ekleyebilir
    [JsonIgnore] public override bool RequiresWorkspaceAccess => true;

    [JsonIgnore] public int JourneyId { get; init; }

    // Yeni durak bilgileri
    public int CustomerId { get; init; }
    public string Address { get; init; }
    public double Latitude { get; init; }
    public double Longitude { get; init; }
    public int? ServiceTimeMinutes { get; init; }
    public string? Notes { get; init; }

    // ✅ YENİ: Zaman aralığı (arrive between)
    public string? ArriveBetweenStart { get; init; } // "HH:mm" format
    public string? ArriveBetweenEnd { get; init; }   // "HH:mm" format
}

public class AddStopToActiveJourneyCommandValidator : AbstractValidator<AddStopToActiveJourneyCommand>
{
    public AddStopToActiveJourneyCommandValidator()
    {
        RuleFor(x => x.JourneyId).GreaterThan(0);
        RuleFor(x => x.CustomerId).GreaterThan(0);
        RuleFor(x => x.Address).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Latitude).InclusiveBetween(-90, 90);
        RuleFor(x => x.Longitude).InclusiveBetween(-180, 180);
        RuleFor(x => x.ServiceTimeMinutes).GreaterThan(0).When(x => x.ServiceTimeMinutes.HasValue);
    }
}

public class AddStopToActiveJourneyCommandHandler : BaseAuthenticatedCommandHandler<AddStopToActiveJourneyCommand, JourneyResponse>
{
    private readonly AppDbContext _context;
    private readonly ISubscriptionService _subscriptionService;
    private readonly IHubContext<JourneyHub> _journeyHub;
    private readonly ILogger<AddStopToActiveJourneyCommandHandler> _logger;

    public AddStopToActiveJourneyCommandHandler(
        AppDbContext context,
        IUserService userService,
        ISubscriptionService subscriptionService,
        IHubContext<JourneyHub> journeyHub,
        ILogger<AddStopToActiveJourneyCommandHandler> logger) : base(userService)
    {
        _context = context;
        _subscriptionService = subscriptionService;
        _journeyHub = journeyHub;
        _logger = logger;
    }

    protected override async Task<JourneyResponse> HandleCommand(AddStopToActiveJourneyCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation($"[ADD_STOP] Adding stop to journey #{request.JourneyId} for customer #{request.CustomerId}");

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

        // Sadece aktif veya planned seferlere durak eklenebilir
        if (journey.IsCompleted || journey.IsCancelled)
            throw new ApiException("Tamamlanmış veya iptal edilmiş seferlere durak eklenemez", 400);

        // Customer kontrolü
        var customer = await _context.Customers
            .FirstOrDefaultAsync(c => c.Id == request.CustomerId && c.WorkspaceId == User.WorkspaceId, cancellationToken);

        if (customer is null)
            throw new ApiException("Müşteri bulunamadı", 404);

        // Trial limiti kontrolü
        var canAddStop = await _subscriptionService.CanAddStop(User.WorkspaceId);
        if (!canAddStop)
        {
            throw new ApiException("Trial limitinize ulaştınız. Devam etmek için plan yükseltin.", 403);
        }

        // Yeni durak order'ı hesapla (son duraktan bir önceki - çünkü son durak depodur)
        var currentMaxOrder = journey.Stops.Max(s => s.Order);
        var newOrder = currentMaxOrder; // Son durağın (depo) yerine ekleyeceğiz

        // Son durağın (depo) order'ını artır
        var lastStop = journey.Stops.FirstOrDefault(s => s.Order == currentMaxOrder);
        if (lastStop != null)
        {
            lastStop.Order = currentMaxOrder + 1;
        }

        // Önce Route'a RouteStop ekle (RouteStopRequest kullanarak)
        var serviceTime = request.ServiceTimeMinutes.HasValue
            ? TimeSpan.FromMinutes(request.ServiceTimeMinutes.Value)
            : journey.Route.Workspace.DefaultServiceTime;

        var routeStopRequest = new Monolith.WebAPI.Requests.RouteStopRequest
        {
            CustomerId = request.CustomerId,
            Name = customer.Name,
            Address = request.Address,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            Notes = request.Notes,
            ContactFullName = customer.Name, // Customer.ContactPerson yok, Name kullan
            ContactPhone = customer.Phone,
            ContactEmail = customer.Email,
            ServiceTime = serviceTime,
            Type = Monolith.WebAPI.Data.Journeys.LocationType.Delivery,
            OrderType = Monolith.WebAPI.Data.Journeys.OrderType.Auto, // Otomatik sıralama
            ProofOfDeliveryRequired = false,
            SignatureRequired = false,
            PhotoRequired = false
        };

        var newRouteStop = new RouteStop(routeStopRequest, journey.RouteId, journey.Route.Workspace.DefaultServiceTime);

        // Order'ı manuel set et (private setter yok)
        typeof(RouteStop).GetProperty("Order")?.SetValue(newRouteStop, journey.Route.Stops.Max(s => s.Order) + 1);

        journey.Route.Stops.Add(newRouteStop);
        await _context.SaveChangesAsync(cancellationToken); // RouteStop'u kaydet ki ID'si oluşsun

        // ✅ Zaman aralığını parse et (eğer varsa)
        TimeSpan? arriveBetweenStart = null;
        TimeSpan? arriveBetweenEnd = null;

        if (!string.IsNullOrEmpty(request.ArriveBetweenStart) && TimeSpan.TryParse(request.ArriveBetweenStart, out var startTime))
        {
            arriveBetweenStart = startTime;
        }

        if (!string.IsNullOrEmpty(request.ArriveBetweenEnd) && TimeSpan.TryParse(request.ArriveBetweenEnd, out var endTime))
        {
            arriveBetweenEnd = endTime;
        }

        // Şimdi JourneyStop oluştur
        var newStop = new JourneyStop
        {
            JourneyId = journey.Id,
            StopId = newRouteStop.Id,
            RouteStopId = newRouteStop.Id,
            Order = newOrder,
            StartAddress = request.Address,
            StartLatitude = request.Latitude,
            StartLongitude = request.Longitude,
            EndAddress = request.Address,
            EndLatitude = request.Latitude,
            EndLongitude = request.Longitude,
            Status = JourneyStopStatus.Pending,
            // ETA'lar optimizasyondan sonra güncellenecek
            EstimatedArrivalTime = TimeSpan.Zero,
            EstimatedDepartureTime = null,
            Distance = 0,
            // ✅ YENİ: Zaman aralığı
            ArriveBetweenStart = arriveBetweenStart,
            ArriveBetweenEnd = arriveBetweenEnd
        };

        journey.Stops.Add(newStop);

        // Journey'e optimizasyon bayrağını set et
        journey.NeedsReoptimization = true;

        await _context.SaveChangesAsync(cancellationToken);

        // Kullanım kaydı
        await _subscriptionService.RecordStopUsage(User.WorkspaceId, 1);

        _logger.LogInformation($"[ADD_STOP] Stop added successfully to journey #{request.JourneyId}, order: {newOrder}");

        // SignalR ile şoföre bildirim gönder
        try
        {
            await _journeyHub.Clients.Group($"journey_{journey.Id}").SendAsync(
                "NewStopAdded",
                new
                {
                    JourneyId = journey.Id,
                    Message = "Yeni durak eklendi. Optimizasyon gerekiyor.",
                    NeedsReoptimization = true,
                    NewStop = new
                    {
                        Order = newStop.Order,
                        CustomerName = customer.Name,
                        Address = request.Address
                    }
                },
                cancellationToken
            );

            _logger.LogInformation($"[ADD_STOP] SignalR notification sent to journey #{journey.Id}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[ADD_STOP] Failed to send SignalR notification for journey #{journey.Id}");
        }

        // Journey'i tekrar yükle (ilişkilerle birlikte)
        journey = await _context.Journeys
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
            .Include(j => j.Statuses)
            .FirstOrDefaultAsync(x => x.Id == request.JourneyId, cancellationToken);

        return new JourneyResponse(journey);
    }
}
