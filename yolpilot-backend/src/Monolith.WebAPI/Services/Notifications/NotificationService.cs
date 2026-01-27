using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Data.Notifications;
using Monolith.WebAPI.Hubs;

namespace Monolith.WebAPI.Services.Notifications;

public class NotificationService : INotificationService
{
    private readonly AppDbContext _context;
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        AppDbContext context,
        IHubContext<NotificationHub> hubContext,
        ILogger<NotificationService> logger)
    {
        _context = context;
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task CreateNotificationAsync(int workspaceId, Guid userId, string title, string message, NotificationType type, object? data = null)
    {
        try
        {
            _logger.LogInformation($"CreateNotificationAsync called - WorkspaceId: {workspaceId}, UserId: {userId}, Title: {title}, Type: {type}");
            
            var notification = new Notification(workspaceId, userId, title, message, type, data);
            
            _logger.LogInformation($"Notification object created - Id: {notification.Id}, WorkspaceId: {notification.WorkspaceId}, UserId: {notification.UserId}");
            
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Notification saved to database - Id: {notification.Id}");

            // Send real-time notification via SignalR
            await _hubContext.Clients.User(userId.ToString()).SendAsync("NewNotification", new
            {
                id = notification.Id,
                title = notification.Title,
                message = notification.Message,
                type = notification.Type.ToString(),
                isRead = notification.IsRead,
                createdAt = notification.CreatedAt,
                data = data
            });

            _logger.LogInformation("Notification created and sent via SignalR for user {UserId}: {Title}", userId, title);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating notification for user {UserId}: {Title}", userId, title);
            throw;
        }
    }

    public async Task CreateJourneyAssignedNotificationAsync(int workspaceId, Guid driverId, int journeyId, string routeName)
    {
        _logger.LogInformation($"CreateJourneyAssignedNotificationAsync called - WorkspaceId: {workspaceId}, DriverId: {driverId}, JourneyId: {journeyId}, RouteName: {routeName}");
        
        var title = "Yeni Görev Atandı";
        var message = $"{routeName} rotası size atandı.";
        var data = new { journeyId, routeName, type = "journey_assigned" };

        await CreateNotificationAsync(workspaceId, driverId, title, message, NotificationType.JOURNEY_ASSIGNED, data);
        
        _logger.LogInformation($"CreateJourneyAssignedNotificationAsync completed - DriverId: {driverId}");
    }

    public async Task CreateJourneyStatusChangedNotificationAsync(int workspaceId, Guid userId, int journeyId, string routeName, string newStatus, string? previousStatus = null)
    {
        var title = "Görev Durumu Değişti";
        var message = previousStatus != null 
            ? $"{routeName} rotasının durumu {previousStatus} -> {newStatus} olarak değişti."
            : $"{routeName} rotasının durumu {newStatus} olarak güncellendi.";
        
        var data = new { journeyId, routeName, newStatus, previousStatus, type = "journey_status_changed" };

        await CreateNotificationAsync(workspaceId, userId, title, message, NotificationType.JOURNEY_STATUS_CHANGED, data);
    }

    public async Task CreateSystemAnnouncementAsync(string title, string message, string? targetRole = null, int? workspaceId = null)
    {
        try
        {
            // Get target users
            var usersQuery = _context.Users.AsQueryable();

            // Filter by workspace if specified
            if (workspaceId.HasValue)
            {
                usersQuery = usersQuery.Where(u => u.WorkspaceId == workspaceId.Value);
            }

            // Filter by role if specified
            if (!string.IsNullOrEmpty(targetRole))
            {
                switch (targetRole.ToLower())
                {
                    case "driver":
                        usersQuery = usersQuery.Where(u => u.IsDriver);
                        break;
                    case "dispatcher":
                        usersQuery = usersQuery.Where(u => u.IsDispatcher);
                        break;
                    case "admin":
                        usersQuery = usersQuery.Where(u => u.IsAdmin);
                        break;
                    case "superadmin":
                        usersQuery = usersQuery.Where(u => u.IsSuperAdmin);
                        break;
                }
            }

            var targetUsers = await usersQuery.ToListAsync();

            // Create notifications for all target users
            foreach (var user in targetUsers)
            {
                await CreateNotificationAsync(user.WorkspaceId, user.Id, title, message, NotificationType.SYSTEM_ANNOUNCEMENT);
            }

            _logger.LogInformation("System announcement sent to {UserCount} users", targetUsers.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating system announcement: {Title}", title);
            throw;
        }
    }

    public async Task CreateDeadlineReminderAsync(int workspaceId, Guid driverId, int journeyId, string routeName, DateTime deadline)
    {
        var title = "Teslim Tarihi Yaklaşıyor";
        var message = $"{routeName} rotasının teslim tarihi yaklaşıyor: {deadline:dd.MM.yyyy HH:mm}";
        var data = new { journeyId, routeName, deadline, type = "deadline_reminder" };

        await CreateNotificationAsync(workspaceId, driverId, title, message, NotificationType.DEADLINE_REMINDER, data);
    }

    public async Task CreatePerformanceAlertAsync(int workspaceId, Guid driverId, string alertMessage, object? data = null)
    {
        var title = "Performans Uyarısı";
        
        await CreateNotificationAsync(workspaceId, driverId, title, alertMessage, NotificationType.PERFORMANCE_ALERT, data);
    }

    public async Task NotifyAllWorkspaceUsersAsync(int workspaceId, string title, string message, NotificationType type, object? data = null)
    {
        try
        {
            var workspaceUsers = await _context.Users
                .Where(u => u.WorkspaceId == workspaceId)
                .ToListAsync();

            foreach (var user in workspaceUsers)
            {
                await CreateNotificationAsync(workspaceId, user.Id, title, message, type, data);
            }

            _logger.LogInformation("Notification sent to all users in workspace {WorkspaceId}: {Title}", workspaceId, title);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error notifying workspace {WorkspaceId} users: {Title}", workspaceId, title);
            throw;
        }
    }

    public async Task NotifyAdminDispatchersAsync(int workspaceId, string title, string message, NotificationType type, object? data = null)
    {
        try
        {
            _logger.LogInformation($"NotifyAdminDispatchersAsync called for workspace {workspaceId}: {title}");
            
            var adminDispatcherUsers = await _context.Users
                .Where(u => u.WorkspaceId == workspaceId && (u.IsAdmin || u.IsDispatcher))
                .ToListAsync();

            _logger.LogInformation($"Found {adminDispatcherUsers.Count} admin/dispatcher users in workspace {workspaceId}");

            foreach (var user in adminDispatcherUsers)
            {
                await CreateNotificationAsync(workspaceId, user.Id, title, message, type, data);
            }

            _logger.LogInformation("Notifications sent to {UserCount} admin/dispatcher users in workspace {WorkspaceId}: {Title}", adminDispatcherUsers.Count, workspaceId, title);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error notifying admin/dispatcher users in workspace {WorkspaceId}: {Title}", workspaceId, title);
            throw;
        }
    }
}