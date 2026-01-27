using Monolith.WebAPI.Data.Workspace;

namespace Monolith.WebAPI.Services.WhatsApp;

public interface IWhatsAppRateLimiter
{
    Task<RateLimitResult> CheckRateLimit(int workspaceId);
    Task RecordMessage(int workspaceId);
    Task<WhatsAppUsageStats> GetUsageStats(int workspaceId);
    Task ResetDailyLimits();
}

public class RateLimitResult
{
    public bool CanSend { get; set; }
    public string Reason { get; set; }
    public int RemainingMessages { get; set; }
    public DateTime? NextAvailableTime { get; set; }
    public bool ShouldQueue { get; set; }
}

public class WhatsAppUsageStats
{
    public int MessagesLastMinute { get; set; }
    public int MessagesLastHour { get; set; }
    public int MessagesToday { get; set; }
    public int MessagesThisMonth { get; set; }
    
    // Limitler
    public int MaxPerMinute { get; set; }
    public int MaxPerHour { get; set; }
    public int MaxPerDay { get; set; }
    public int MaxPerMonth { get; set; }
    
    // Kalan
    public int RemainingToday { get; set; }
    public int RemainingThisMonth { get; set; }
    
    // Twilio API Limitleri
    public int TwilioRemainingRequests { get; set; }
    public DateTime TwilioResetTime { get; set; }
}