namespace Monolith.WebAPI.Services.WhatsApp;

public interface IWhatsAppQueueService
{
    Task<string> QueueMessage(WhatsAppQueueItem item);
    Task ProcessQueue();
    Task<List<WhatsAppQueueItem>> GetPendingMessages(int workspaceId);
    Task CancelMessage(string queueId);
}

public class WhatsAppQueueItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public int WorkspaceId { get; set; }
    public MessageType Type { get; set; }
    public string ToNumber { get; set; }
    public Dictionary<string, string> Parameters { get; set; }
    public DateTime QueuedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }
    public int RetryCount { get; set; }
    public string Status { get; set; } = "Pending"; // Pending, Processing, Sent, Failed
    public string ErrorMessage { get; set; }
}

public enum MessageType
{
    JourneyStarted,
    Approaching,
    DeliveryCompleted,
    DeliveryFailed,
    Test
}