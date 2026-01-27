using Monolith.WebAPI.Data.Notifications;

namespace Monolith.WebAPI.Responses.Notifications;

public class NotificationResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; } = "";
    public string Message { get; set; } = "";
    public string Type { get; set; } = "";
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ReadAt { get; set; }
    public object? Data { get; set; }

    public static NotificationResponse FromEntity(Notification notification)
    {
        return new NotificationResponse
        {
            Id = notification.Id,
            Title = notification.Title,
            Message = notification.Message,
            Type = notification.Type.ToString(),
            IsRead = notification.IsRead,
            CreatedAt = notification.CreatedAt,
            ReadAt = notification.ReadAt,
            Data = string.IsNullOrEmpty(notification.Data) 
                ? null 
                : System.Text.Json.JsonSerializer.Deserialize<object>(notification.Data)
        };
    }
}

public class NotificationsListResponse
{
    public List<NotificationResponse> Notifications { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;
}

public class UnreadCountResponse
{
    public int UnreadCount { get; set; }
}