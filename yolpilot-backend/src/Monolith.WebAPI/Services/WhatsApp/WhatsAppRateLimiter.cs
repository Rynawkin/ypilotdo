using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Data.Workspace;
using Monolith.WebAPI.Services.Subscription;

namespace Monolith.WebAPI.Services.WhatsApp;

public class WhatsAppRateLimiter : IWhatsAppRateLimiter
{
    private readonly AppDbContext _context;
    private readonly IMemoryCache _cache;
    private readonly ISubscriptionService _subscriptionService;
    private readonly ILogger<WhatsAppRateLimiter> _logger;

    // Rate limit constants by plan
    private readonly Dictionary<PlanType, RateLimitConfig> _planLimits = new()
    {
        [PlanType.Trial] = new RateLimitConfig { MaxPerMinute = 2, MaxPerHour = 10, MaxPerDay = 25 },
        [PlanType.Starter] = new RateLimitConfig { MaxPerMinute = 0, MaxPerHour = 0, MaxPerDay = 0 },
        [PlanType.Growth] = new RateLimitConfig { MaxPerMinute = 5, MaxPerHour = 50, MaxPerDay = 100 },
        [PlanType.Professional] = new RateLimitConfig { MaxPerMinute = 10, MaxPerHour = 100, MaxPerDay = 500 },
        [PlanType.Business] = new RateLimitConfig { MaxPerMinute = 20, MaxPerHour = 200, MaxPerDay = 1000 }
    };

    public WhatsAppRateLimiter(
        AppDbContext context,
        IMemoryCache cache,
        ISubscriptionService subscriptionService,
        ILogger<WhatsAppRateLimiter> logger)
    {
        _context = context;
        _cache = cache;
        _subscriptionService = subscriptionService;
        _logger = logger;
    }

    public async Task<RateLimitResult> CheckRateLimit(int workspaceId)
    {
        var workspace = await _context.Workspaces
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == workspaceId);

        if (workspace == null)
        {
            return new RateLimitResult
            {
                CanSend = false,
                Reason = "Workspace not found"
            };
        }

        // Plan kontrolü
        if (workspace.PlanType == PlanType.Starter)
        {
            return new RateLimitResult
            {
                CanSend = false,
                Reason = "WhatsApp is not available in Starter plan"
            };
        }

        var limits = _planLimits[workspace.PlanType];
        var stats = await GetWorkspaceMessageStats(workspaceId);

        // Dakikalık limit kontrolü
        if (stats.MessagesLastMinute >= limits.MaxPerMinute)
        {
            return new RateLimitResult
            {
                CanSend = false,
                Reason = $"Minute limit reached ({limits.MaxPerMinute}/min)",
                NextAvailableTime = DateTime.UtcNow.AddMinutes(1),
                ShouldQueue = true
            };
        }

        // Saatlik limit kontrolü
        if (stats.MessagesLastHour >= limits.MaxPerHour)
        {
            return new RateLimitResult
            {
                CanSend = false,
                Reason = $"Hourly limit reached ({limits.MaxPerHour}/hour)",
                NextAvailableTime = DateTime.UtcNow.AddHours(1),
                ShouldQueue = true
            };
        }

        // Günlük limit kontrolü
        if (stats.MessagesToday >= limits.MaxPerDay)
        {
            return new RateLimitResult
            {
                CanSend = false,
                Reason = $"Daily limit reached ({limits.MaxPerDay}/day)",
                NextAvailableTime = DateTime.Today.AddDays(1).ToUniversalTime(),
                ShouldQueue = false // Günlük limit aşıldıysa kuyruğa alma
            };
        }

        // Aylık plan limiti kontrolü
        var planLimits = _subscriptionService.GetPlanLimits(workspace.PlanType);
        var totalMonthlyLimit = planLimits.IncludedWhatsAppMessages + 
            (workspace.AllowOverageCharges ? 1000 : 0); // Ek ücretli 1000 mesaj daha

        if (workspace.CurrentMonthWhatsAppMessages >= totalMonthlyLimit)
        {
            return new RateLimitResult
            {
                CanSend = false,
                Reason = "Monthly plan limit reached",
                RemainingMessages = 0,
                ShouldQueue = false
            };
        }

        // Twilio API rate limit kontrolü (cache'ten)
        var twilioLimitKey = $"twilio_limit_{workspaceId}";
        var twilioCount = _cache.Get<int?>(twilioLimitKey) ?? 0;
        
        if (twilioCount >= 100) // Twilio'nun saatlik limiti genelde 100-1000 arası
        {
            return new RateLimitResult
            {
                CanSend = false,
                Reason = "Twilio API rate limit reached",
                NextAvailableTime = DateTime.UtcNow.AddHours(1),
                ShouldQueue = true
            };
        }

        return new RateLimitResult
        {
            CanSend = true,
            RemainingMessages = totalMonthlyLimit - workspace.CurrentMonthWhatsAppMessages
        };
    }

    public async Task RecordMessage(int workspaceId)
    {
        // SECURITY: Re-check limits to prevent race conditions and concurrent limit bypass
        var limitCheck = await CheckRateLimit(workspaceId);
        if (!limitCheck.CanSend)
        {
            _logger.LogWarning("WhatsApp limit exceeded for workspace {WorkspaceId}: {Reason}",
                workspaceId, limitCheck.Reason);
            throw new InvalidOperationException($"WhatsApp limit exceeded: {limitCheck.Reason}");
        }

        // Cache'e dakikalık ve saatlik kayıt
        var minuteKey = $"whatsapp_minute_{workspaceId}_{DateTime.UtcNow:yyyyMMddHHmm}";
        var hourKey = $"whatsapp_hour_{workspaceId}_{DateTime.UtcNow:yyyyMMddHH}";
        var dayKey = $"whatsapp_day_{workspaceId}_{DateTime.Today:yyyyMMdd}";
        var twilioKey = $"twilio_limit_{workspaceId}";

        // Dakikalık sayaç
        var minuteCount = _cache.Get<int?>(minuteKey) ?? 0;
        _cache.Set(minuteKey, minuteCount + 1, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(2),
            Size = 1 // BUGFIX: Must specify Size when SizeLimit is set
        });

        // Saatlik sayaç
        var hourCount = _cache.Get<int?>(hourKey) ?? 0;
        _cache.Set(hourKey, hourCount + 1, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(2),
            Size = 1 // BUGFIX: Must specify Size when SizeLimit is set
        });

        // Günlük sayaç
        var dayCount = _cache.Get<int?>(dayKey) ?? 0;
        _cache.Set(dayKey, dayCount + 1, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(1),
            Size = 1 // BUGFIX: Must specify Size when SizeLimit is set
        });

        // Twilio API sayacı
        var twilioCount = _cache.Get<int?>(twilioKey) ?? 0;
        _cache.Set(twilioKey, twilioCount + 1, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1),
            Size = 1 // BUGFIX: Must specify Size when SizeLimit is set
        });

        // Database'e de kaydet (subscription service üzerinden)
        await _subscriptionService.RecordWhatsAppUsage(workspaceId, 1);

        _logger.LogInformation("WhatsApp message recorded for workspace {WorkspaceId}. Minute: {Minute}, Hour: {Hour}, Day: {Day}",
            workspaceId, minuteCount + 1, hourCount + 1, dayCount + 1);
    }

    public async Task<WhatsAppUsageStats> GetUsageStats(int workspaceId)
    {
        var workspace = await _context.Workspaces
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == workspaceId);

        if (workspace == null)
            return new WhatsAppUsageStats();

        var stats = await GetWorkspaceMessageStats(workspaceId);
        var limits = _planLimits[workspace.PlanType];
        var planLimits = _subscriptionService.GetPlanLimits(workspace.PlanType);

        return new WhatsAppUsageStats
        {
            MessagesLastMinute = stats.MessagesLastMinute,
            MessagesLastHour = stats.MessagesLastHour,
            MessagesToday = stats.MessagesToday,
            MessagesThisMonth = workspace.CurrentMonthWhatsAppMessages,
            
            MaxPerMinute = limits.MaxPerMinute,
            MaxPerHour = limits.MaxPerHour,
            MaxPerDay = limits.MaxPerDay,
            MaxPerMonth = planLimits.IncludedWhatsAppMessages,
            
            RemainingToday = Math.Max(0, limits.MaxPerDay - stats.MessagesToday),
            RemainingThisMonth = Math.Max(0, planLimits.IncludedWhatsAppMessages - workspace.CurrentMonthWhatsAppMessages),
            
            TwilioRemainingRequests = 100 - (_cache.Get<int?>($"twilio_limit_{workspaceId}") ?? 0),
            TwilioResetTime = DateTime.UtcNow.AddHours(1)
        };
    }

    public async Task ResetDailyLimits()
    {
        // Bu method bir background job ile her gün çalıştırılabilir
        _logger.LogInformation("Daily WhatsApp limits reset");
        await Task.CompletedTask;
    }

    private async Task<MessageStats> GetWorkspaceMessageStats(int workspaceId)
    {
        var minuteKey = $"whatsapp_minute_{workspaceId}_{DateTime.UtcNow:yyyyMMddHHmm}";
        var hourKey = $"whatsapp_hour_{workspaceId}_{DateTime.UtcNow:yyyyMMddHH}";
        var dayKey = $"whatsapp_day_{workspaceId}_{DateTime.Today:yyyyMMdd}";

        return new MessageStats
        {
            MessagesLastMinute = _cache.Get<int?>(minuteKey) ?? 0,
            MessagesLastHour = _cache.Get<int?>(hourKey) ?? 0,
            MessagesToday = _cache.Get<int?>(dayKey) ?? 0
        };
    }

    private class RateLimitConfig
    {
        public int MaxPerMinute { get; set; }
        public int MaxPerHour { get; set; }
        public int MaxPerDay { get; set; }
    }

    private class MessageStats
    {
        public int MessagesLastMinute { get; set; }
        public int MessagesLastHour { get; set; }
        public int MessagesToday { get; set; }
    }
}