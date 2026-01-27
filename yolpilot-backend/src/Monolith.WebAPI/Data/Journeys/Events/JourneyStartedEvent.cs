// src/Monolith.WebAPI/Data/Journeys/Events/JourneyStartedEvent.cs

using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Monolith.WebAPI.Applications.Events;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Data.Notifications;
using Monolith.WebAPI.Services.Notifications;

namespace Monolith.WebAPI.Data.Journeys.Events;

public class JourneyStartedEvent : NotificationEvent
{
    public int JourneyId { get; set; }
    public DateTime StartedAt { get; set; }
    
    public JourneyStartedEvent(int journeyId, DateTime? startedAt = null)
    {
        JourneyId = journeyId;
        StartedAt = startedAt ?? DateTime.UtcNow;
    }
}

public class JourneyStartedEventHandler : INotificationHandler<JourneyStartedEvent>
{
    private readonly AppDbContext _context;
    private readonly INotificationService _notificationService;
    private readonly ILogger<JourneyStartedEventHandler> _logger;

    public JourneyStartedEventHandler(
        AppDbContext context,
        INotificationService notificationService,
        ILogger<JourneyStartedEventHandler> logger)
    {
        _context = context;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task Handle(JourneyStartedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation($"JourneyStartedEvent received for Journey {notification.JourneyId}");

            var journey = await _context.Journeys
                .Include(j => j.Route)
                    .ThenInclude(r => r.Driver)
                        .ThenInclude(d => d.User)
                .FirstOrDefaultAsync(j => j.Id == notification.JourneyId, cancellationToken);

            if (journey?.Route?.Driver?.User != null)
            {
                // Driver'a bildir
                await _notificationService.CreateJourneyStatusChangedNotificationAsync(
                    journey.Route.WorkspaceId,
                    journey.Route.Driver.User.Id,
                    journey.Id,
                    journey.Route.Name,
                    "Başlatıldı"
                );

                // Admin/Dispatcher'lara bildir
                await _notificationService.NotifyAdminDispatchersAsync(
                    journey.Route.WorkspaceId,
                    "Görev Başlatıldı",
                    $"{journey.Route.Driver.Name} sürücüsü {journey.Route.Name} rotasına başladı.",
                    NotificationType.JOURNEY_STATUS_CHANGED,
                    new { journeyId = journey.Id, driverName = journey.Route.Driver.Name, routeName = journey.Route.Name, status = "started" }
                );

                _logger.LogInformation($"Journey started notifications sent for Journey {notification.JourneyId}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error handling JourneyStartedEvent for Journey {notification.JourneyId}");
        }
    }
}