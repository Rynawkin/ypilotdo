using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Monolith.WebAPI.Data.Members;

namespace Monolith.WebAPI.Data.Notifications;

public class Notification
{
    public Notification()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        IsRead = false;
    }

    public Notification(int workspaceId, Guid userId, string title, string message, NotificationType type, object? data = null)
    {
        Id = Guid.NewGuid();
        WorkspaceId = workspaceId;
        UserId = userId;
        Title = title;
        Message = message;
        Type = type;
        IsRead = false;
        CreatedAt = DateTime.UtcNow;
        Data = data != null ? System.Text.Json.JsonSerializer.Serialize(data) : null;
    }

    [Key]
    public Guid Id { get; set; }

    [Required]
    public int WorkspaceId { get; set; }

    [Required]
    public Guid UserId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = "";

    [Required]
    [MaxLength(1000)]
    public string Message { get; set; } = "";

    [Column(TypeName = "nvarchar(50)")]
    public NotificationType Type { get; set; }

    public bool IsRead { get; set; } = false;

    public DateTime? ReadAt { get; set; }

    public DateTime CreatedAt { get; set; }
    
    public DateTime? UpdatedAt { get; set; }

    public bool IsDeleted { get; set; } = false;

    [MaxLength(2000)] // JSON data
    public string? Data { get; set; }

    // Navigation Properties
    [ForeignKey("UserId")]
    public ApplicationUser User { get; set; } = null!;

    public void MarkAsRead()
    {
        IsRead = true;
        ReadAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public T? GetData<T>() where T : class
    {
        if (string.IsNullOrEmpty(Data))
            return null;

        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<T>(Data);
        }
        catch
        {
            return null;
        }
    }
}

public enum NotificationType
{
    JOURNEY_ASSIGNED,
    JOURNEY_STATUS_CHANGED,
    SYSTEM_ANNOUNCEMENT,
    ROUTE_CANCELLED,
    DEADLINE_REMINDER,
    PERFORMANCE_ALERT,
    MAINTENANCE_REMINDER,
    CUSTOMER_FEEDBACK
}