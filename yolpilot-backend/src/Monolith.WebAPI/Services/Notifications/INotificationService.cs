using Monolith.WebAPI.Data.Notifications;

namespace Monolith.WebAPI.Services.Notifications;

public interface INotificationService
{
    Task CreateNotificationAsync(int workspaceId, Guid userId, string title, string message, NotificationType type, object? data = null);
    Task CreateJourneyAssignedNotificationAsync(int workspaceId, Guid driverId, int journeyId, string routeName);
    Task CreateJourneyStatusChangedNotificationAsync(int workspaceId, Guid userId, int journeyId, string routeName, string newStatus, string? previousStatus = null);
    Task CreateSystemAnnouncementAsync(string title, string message, string? targetRole = null, int? workspaceId = null);
    Task CreateDeadlineReminderAsync(int workspaceId, Guid driverId, int journeyId, string routeName, DateTime deadline);
    Task CreatePerformanceAlertAsync(int workspaceId, Guid driverId, string alertMessage, object? data = null);
    Task NotifyAllWorkspaceUsersAsync(int workspaceId, string title, string message, NotificationType type, object? data = null);
    Task NotifyAdminDispatchersAsync(int workspaceId, string title, string message, NotificationType type, object? data = null);
}