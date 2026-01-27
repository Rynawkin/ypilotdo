// src/Monolith.WebAPI/Data/Journeys/Events/JourneyFinishedEvent.cs

using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Monolith.WebAPI.Applications.Events;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Data.Notifications;
using Monolith.WebAPI.Services.Notifications;

namespace Monolith.WebAPI.Data.Journeys.Events;

public class JourneyFinishedEvent : NotificationEvent
{
    public int JourneyId { get; set; }
    public DateTime FinishedAt { get; set; }
    
    public JourneyFinishedEvent(int journeyId, DateTime? finishedAt = null)
    {
        JourneyId = journeyId;
        FinishedAt = finishedAt ?? DateTime.UtcNow;
    }
}

public class JourneyFinishedEventHandler : INotificationHandler<JourneyFinishedEvent>
{
    private readonly AppDbContext _context;
    private readonly INotificationService _notificationService;
    private readonly ILogger<JourneyFinishedEventHandler> _logger;

    public JourneyFinishedEventHandler(
        AppDbContext context,
        INotificationService notificationService,
        ILogger<JourneyFinishedEventHandler> logger)
    {
        _context = context;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task Handle(JourneyFinishedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation($"JourneyFinishedEvent received for Journey {notification.JourneyId}");

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
                    "Tamamlandı"
                );

                // Admin/Dispatcher'lara bildir
                await _notificationService.NotifyAdminDispatchersAsync(
                    journey.Route.WorkspaceId,
                    "Görev Tamamlandı",
                    $"{journey.Route.Driver.Name} sürücüsü {journey.Route.Name} rotasını tamamladı.",
                    NotificationType.JOURNEY_STATUS_CHANGED,
                    new { journeyId = journey.Id, driverName = journey.Route.Driver.Name, routeName = journey.Route.Name, status = "completed" }
                );

                _logger.LogInformation($"Journey finished notifications sent for Journey {notification.JourneyId}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error handling JourneyFinishedEvent for Journey {notification.JourneyId}");
        }
    }
}