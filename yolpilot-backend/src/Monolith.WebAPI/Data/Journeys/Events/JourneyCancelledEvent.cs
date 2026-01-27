// src/Monolith.WebAPI/Data/Journeys/Events/JourneyCancelledEvent.cs

using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Monolith.WebAPI.Applications.Events;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Data.Notifications;
using Monolith.WebAPI.Services.Notifications;

namespace Monolith.WebAPI.Data.Journeys.Events;

public class JourneyCancelledEvent : NotificationEvent
{
    public int JourneyId { get; set; }
    public DateTime? CancelledAt { get; set; }
    
    public JourneyCancelledEvent(int journeyId, DateTime? cancelledAt = null)
    {
        JourneyId = journeyId;
        CancelledAt = cancelledAt ?? DateTime.UtcNow;
    }
}

public class JourneyCancelledEventHandler : INotificationHandler<JourneyCancelledEvent>
{
    private readonly AppDbContext _context;
    private readonly INotificationService _notificationService;
    private readonly ILogger<JourneyCancelledEventHandler> _logger;

    public JourneyCancelledEventHandler(
        AppDbContext context,
        INotificationService notificationService,
        ILogger<JourneyCancelledEventHandler> logger)
    {
        _context = context;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task Handle(JourneyCancelledEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation($"JourneyCancelledEvent received for Journey {notification.JourneyId}");

            var journey = await _context.Journeys
                .Include(j => j.Route)
                    .ThenInclude(r => r.Driver)
                        .ThenInclude(d => d.User)
                .FirstOrDefaultAsync(j => j.Id == notification.JourneyId, cancellationToken);

            if (journey?.Route?.Driver?.User != null)
            {
                // Driver'a bildir
                await _notificationService.CreateNotificationAsync(
                    journey.Route.WorkspaceId,
                    journey.Route.Driver.User.Id,
                    "Görev İptal Edildi",
                    $"{journey.Route.Name} rotası iptal edildi.",
                    NotificationType.ROUTE_CANCELLED,
                    new { journeyId = journey.Id, routeName = journey.Route.Name, type = "journey_cancelled" }
                );

                // Admin/Dispatcher'lara bildir
                await _notificationService.NotifyAdminDispatchersAsync(
                    journey.Route.WorkspaceId,
                    "Görev İptal Edildi",
                    $"{journey.Route.Driver.Name} sürücüsünün {journey.Route.Name} rotası iptal edildi.",
                    NotificationType.ROUTE_CANCELLED,
                    new { journeyId = journey.Id, driverName = journey.Route.Driver.Name, routeName = journey.Route.Name, status = "cancelled" }
                );

                _logger.LogInformation($"Journey cancelled notifications sent for Journey {notification.JourneyId}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error handling JourneyCancelledEvent for Journey {notification.JourneyId}");
        }
    }
}