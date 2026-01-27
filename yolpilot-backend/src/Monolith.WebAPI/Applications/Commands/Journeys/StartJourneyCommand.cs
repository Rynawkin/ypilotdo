// src/Monolith.WebAPI/Applications/Commands/Journeys/StartJourneyCommand.cs

using System.Text.Json.Serialization;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Data.Workspace.Enums;
using Monolith.WebAPI.Infrastructure;
using Monolith.WebAPI.Responses.Journeys;
using Monolith.WebAPI.Services.Members;
using Monolith.WebAPI.Services.Email;
using Monolith.WebAPI.Services.Subscription;
using Monolith.WebAPI.Services.WhatsApp;

namespace Monolith.WebAPI.Applications.Commands.Journeys;

public class StartJourneyCommand : BaseAuthenticatedCommand<JourneyResponse>
{
    [JsonIgnore] public override bool RequiresDriver => true;
    [JsonIgnore] public override bool RequiresWorkspaceAccess => true;
    public int JourneyId { get; set; }
}

public class StartJourneyCommandValidator : AbstractValidator<StartJourneyCommand>
{
    public StartJourneyCommandValidator()
    {
        RuleFor(x => x.JourneyId).GreaterThan(0);
    }
}

public class StartJourneyCommandHandler : BaseAuthenticatedCommandHandler<StartJourneyCommand, JourneyResponse>
{
    private readonly AppDbContext _context;
    private readonly IEmailService _emailService;
    private readonly IWhatsAppService _whatsAppService;
    private readonly ISubscriptionService _subscriptionService;
    private readonly ILogger<StartJourneyCommandHandler> _logger;
    private readonly MediatR.ISender _sender;

    public StartJourneyCommandHandler(
        AppDbContext context,
        IUserService userService,
        IEmailService emailService,
        IWhatsAppService whatsAppService,
        ISubscriptionService subscriptionService,
        ILogger<StartJourneyCommandHandler> logger,
        MediatR.ISender sender)
        : base(userService)
    {
        _context = context;
        _emailService = emailService;
        _whatsAppService = whatsAppService;
        _subscriptionService = subscriptionService;
        _logger = logger;
        _sender = sender;
    }

    protected override async Task<JourneyResponse> HandleCommand(StartJourneyCommand request, CancellationToken cancellationToken)
    {
        var journey = await _context.Journeys
            .Include(x => x.Route)
                .ThenInclude(r => r.Stops)
                    .ThenInclude(s => s.Customer)
            .Include(x => x.Driver)
                .ThenInclude(d => d.User)
            .Include(x => x.Vehicle) // ✅ YENİ - Vehicle'ı include et (StartKm için)
            .Include(x => x.Stops)
                .ThenInclude(s => s.RouteStop!)
                    .ThenInclude(rs => rs!.Customer)
            .FirstOrDefaultAsync(x => x.Id == request.JourneyId && x.Route.WorkspaceId == User.WorkspaceId, cancellationToken);

        if (journey == null)
            throw new ApiException("Journey not found.", 404);

        if (journey.StartedAt.HasValue)
            throw new ApiException("Journey is already started.", 400);

        // Workspace settings'i al
        var workspace = await _context.Workspaces
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == User.WorkspaceId, cancellationToken);

        // EÄŸer duraklar henÃ¼z sayÄ±lmadÄ±ysa, ÅŸimdi say (backup mekanizma)
        if (!journey.StopsCountedForBilling && journey.Stops != null && journey.Stops.Any())
        {
            var activeStopCount = journey.Stops.Count(s => 
                s.RouteStop == null || !s.RouteStop.IsExcluded);
            
            if (activeStopCount > 0)
            {
                await _subscriptionService.RecordStopUsage(User.WorkspaceId, activeStopCount);
                journey.MarkStopsAsCounted();
                
                _logger.LogInformation($"Recorded {activeStopCount} stops for billing at journey start (Journey #{journey.Id}, Workspace #{User.WorkspaceId})");
            }
        }

        // Journey'i başlat
        journey.Start();

        // ✅ YENİ: Sefer başlangıç kilometresini kaydet
        if (journey.Vehicle != null && journey.Vehicle.CurrentKm.HasValue)
        {
            journey.StartKm = journey.Vehicle.CurrentKm.Value;
            journey.StartFuel = null; // Başlangıç yakıtı henüz girilmemiş
            _logger.LogInformation("Set journey {JourneyId} StartKm to {Km} from vehicle {VehicleId}",
                journey.Id, journey.StartKm, journey.Vehicle.Id);
        }
        else
        {
            _logger.LogWarning("Journey {JourneyId} started without StartKm (vehicle CurrentKm not set)",
                journey.Id);
        }

        // ✅ YENİ: Tüm duraklar için Original ETA'ları sakla (delay tracking için)
        if (journey.Stops != null && journey.Stops.Any())
        {
            foreach (var stop in journey.Stops)
            {
                if (stop.OriginalEstimatedArrivalTime == TimeSpan.Zero)
                {
                    stop.OriginalEstimatedArrivalTime = stop.EstimatedArrivalTime;
                    stop.OriginalEstimatedDepartureTime = stop.EstimatedDepartureTime ?? TimeSpan.Zero;
                }
            }
            _logger.LogInformation($"Set original ETAs for {journey.Stops.Count} stops in journey {journey.Id}");
        }

        await _context.SaveChangesAsync(cancellationToken);

        // ✅ BUGFIX: Sefer başlatıldığında ETA'ları güncel zamanla yeniden hesapla
        _logger.LogInformation("Recalculating ETAs based on current time for journey {JourneyId}", journey.Id);
        try
        {
            var optimizeCommand = new OptimizeRouteCommand
            {
                RouteId = journey.RouteId,
                PreserveOrder = true, // Sıralamayı değiştirme, sadece ETA hesapla
                IsTimeDeviationOptimization = true, // Yetki kontrolünü atla
                AuthenticatedUserId = request.AuthenticatedUserId
            };

            // OptimizeRouteCommand handler'ını çağır - ETA'ları güncelleyecek
            await _sender.Send(optimizeCommand, cancellationToken);

            _logger.LogInformation("ETAs recalculated successfully for journey {JourneyId}", journey.Id);

            // Journey'i tekrar yükle ki güncellenmiş ETA'lar response'a yansısın
            await _context.Entry(journey).ReloadAsync(cancellationToken);
            await _context.Entry(journey).Collection(j => j.Stops).LoadAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to recalculate ETAs for journey {JourneyId}", journey.Id);
            // ETA hesaplaması başarısız olsa bile sefer başlasın
        }

        // MÃ¼ÅŸterilere email ve WhatsApp gÃ¶nder (excluded olmayan stops iÃ§in)
        if (journey.Stops != null && journey.Stops.Any())
        {
            var driverName = journey.Driver?.Name ?? "SÃ¼rÃ¼cÃ¼mÃ¼z";
            var emailTasks = new List<Task>();
            var whatsAppTasks = new List<Task>();

            // WhatsApp ayarlarÄ±nÄ± kontrol et - YENÄ° YAKLAÅžIM
            bool whatsAppEnabled = workspace.WhatsAppMode != WhatsAppMode.Disabled && 
                                  workspace.WhatsAppNotifyJourneyStart;
            
            // Plan WhatsApp'a izin veriyor mu?
            if (whatsAppEnabled)
            {
                var canSendWhatsApp = await _subscriptionService.CanSendWhatsApp(User.WorkspaceId);
                if (!canSendWhatsApp)
                {
                    _logger.LogWarning("WhatsApp disabled for workspace {WorkspaceId} due to limits", User.WorkspaceId);
                }
                whatsAppEnabled = canSendWhatsApp;
            }

            // Her customer için sadece 1 kere email göndermek için unique customer'ları al
            var uniqueCustomers = journey.Stops
                .Where(s => s.RouteStop?.Customer != null && !s.RouteStop.IsExcluded)
                .Select(s => s.RouteStop.Customer)
                .GroupBy(c => c.Id)
                .Select(g => new {
                    Customer = g.First(),
                    EstimatedTime = journey.Date.Date.Add(g.Min(c =>
                        journey.Stops.First(s => s.RouteStop.Customer.Id == c.Id).EstimatedArrivalTime
                    )).ToString("HH:mm")
                })
                .ToList();

            foreach (var item in uniqueCustomers)
            {
                var customer = item.Customer;
                var estimatedTime = item.EstimatedTime;

                // Sadece rol bazlı email gönder
                emailTasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        await _emailService.SendJourneyStartedToCustomerContactsAsync(
                            customer.Id,
                            customer.Name,
                            driverName,
                            estimatedTime
                        );
                        _logger.LogInformation($"Journey started role-based email sent for customer {customer.Name} (ID: {customer.Id})");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Failed to send role-based email for customer ID {customer.Id}");
                    }
                }));

                // WhatsApp kontrolÃ¼ ve gÃ¶nderimi
                if (whatsAppEnabled && 
                    customer.WhatsAppOptIn && 
                    !string.IsNullOrEmpty(customer.WhatsApp))
                {
                    whatsAppTasks.Add(Task.Run(async () =>
                    {
                        try
                        {
                            var success = await _whatsAppService.SendJourneyStartedMessage(
                                workspace,
                                customer.WhatsApp,
                                customer.Name,
                                driverName,
                                estimatedTime
                            );

                            if (success)
                            {
                                _logger.LogInformation($"Journey started WhatsApp sent to customer {customer.Name} ({customer.WhatsApp})");
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"Failed to send WhatsApp to customer {customer.WhatsApp}");
                        }
                    }));
                }
            }

            // TÃ¼m bildirimleri paralel gÃ¶nder
            var allTasks = emailTasks.Concat(whatsAppTasks).ToList();
            if (allTasks.Any())
            {
                await Task.WhenAll(allTasks);
            }
        }

        return new JourneyResponse(journey);
    }
}