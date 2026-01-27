// src/Monolith.WebAPI/Services/Subscription/SubscriptionService.cs

using Microsoft.EntityFrameworkCore;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Data.Workspace;

namespace Monolith.WebAPI.Services.Subscription;

public class SubscriptionService : ISubscriptionService
{
    private readonly AppDbContext _context;
    private readonly ILogger<SubscriptionService> _logger;

    public SubscriptionService(AppDbContext context, ILogger<SubscriptionService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public PlanLimits GetPlanLimits(PlanType planType)
    {
        return planType switch
        {
            PlanType.Trial => new PlanLimits
            {
                PlanType = PlanType.Trial,
                MonthlyPrice = 0,
                MaxDrivers = 2,
                MaxVehicles = 1,
                MaxCustomers = 50,
                MaxUsers = 1,
                IncludedMonthlyStops = 100,
                AdditionalStopPrice = 0,
                HasCustomerWhatsAppNotifications = true,
                IncludedWhatsAppMessages = 25,
                AdditionalWhatsAppPrice = 0,
                HasTimeWindows = false,
                HasCustomerSatisfactionReport = false,
                HasRouteTemplates = true,
                HasCustomReports = false,
                ProofArchiveDays = 7
            },
            PlanType.Starter => new PlanLimits
            {
                PlanType = PlanType.Starter,
                MonthlyPrice = 850,
                MaxDrivers = 3,
                MaxVehicles = 3,
                MaxCustomers = 100,
                MaxUsers = 2,
                IncludedMonthlyStops = 500,
                AdditionalStopPrice = 3,
                HasCustomerWhatsAppNotifications = false,
                IncludedWhatsAppMessages = 0,
                AdditionalWhatsAppPrice = 0,
                HasTimeWindows = false,
                HasCustomerSatisfactionReport = false,
                HasRouteTemplates = true,
                HasCustomReports = false,
                ProofArchiveDays = 30
            },
            PlanType.Growth => new PlanLimits
            {
                PlanType = PlanType.Growth,
                MonthlyPrice = 1250,
                MaxDrivers = null,
                MaxVehicles = null,
                MaxCustomers = 1000,
                MaxUsers = 10,
                IncludedMonthlyStops = 500,
                AdditionalStopPrice = 3,
                HasCustomerWhatsAppNotifications = true,
                IncludedWhatsAppMessages = 100,
                AdditionalWhatsAppPrice = 0.50m,
                HasTimeWindows = true,
                HasCustomerSatisfactionReport = true,
                HasRouteTemplates = true,
                HasCustomReports = false,
                ProofArchiveDays = 90
            },
            PlanType.Professional => new PlanLimits
            {
                PlanType = PlanType.Professional,
                MonthlyPrice = 2400,
                MaxDrivers = null,
                MaxVehicles = null,
                MaxCustomers = 1000,
                MaxUsers = 10,
                IncludedMonthlyStops = 2000,
                AdditionalStopPrice = 2,
                HasCustomerWhatsAppNotifications = true,
                IncludedWhatsAppMessages = 100,
                AdditionalWhatsAppPrice = 0.50m,
                HasTimeWindows = true,
                HasCustomerSatisfactionReport = true,
                HasRouteTemplates = true,
                HasCustomReports = false,
                ProofArchiveDays = 90
            },
            PlanType.Business => new PlanLimits
            {
                PlanType = PlanType.Business,
                MonthlyPrice = 5900,
                MaxDrivers = null,
                MaxVehicles = null,
                MaxCustomers = null,
                MaxUsers = 50,
                IncludedMonthlyStops = 5000,
                AdditionalStopPrice = 1.5m,
                HasCustomerWhatsAppNotifications = true,
                IncludedWhatsAppMessages = 500,
                AdditionalWhatsAppPrice = 0.30m,
                HasTimeWindows = true,
                HasCustomerSatisfactionReport = true,
                HasRouteTemplates = true,
                HasCustomReports = true,
                ProofArchiveDays = 365
            },
            _ => throw new ArgumentException($"Unknown plan type: {planType}")
        };
    }

    public async Task<bool> CanAddStop(int workspaceId)
    {
        var workspace = await _context.Workspaces
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == workspaceId);
        
        if (workspace == null) return false;

        // Trial kullanıcıları için sert limit kontrolü
        if (workspace.PlanType == PlanType.Trial)
        {
            // Ay kontrolü
            var currentMonth = DateTime.UtcNow;
            if (workspace.LastStopResetDate.Month != currentMonth.Month ||
                workspace.LastStopResetDate.Year != currentMonth.Year)
            {
                // Yeni ay başında limit sıfırlanmış sayılır
                return true;
            }

            var limits = GetPlanLimits(workspace.PlanType);
            
            // Trial limitine ulaşmışsa durdurmayı engelle
            if (workspace.CurrentMonthStops >= limits.IncludedMonthlyStops)
            {
                _logger.LogWarning(
                    "Trial workspace {WorkspaceId} reached stop limit: {Current}/{Limit}",
                    workspaceId, workspace.CurrentMonthStops, limits.IncludedMonthlyStops);
                return false;
            }
        }

        // Diğer planlar için durak eklemeyi engellemiyoruz, sadece ek ücret hesaplanacak
        return true;
    }

    public async Task RecordStopUsage(int workspaceId, int count = 1)
    {
        var workspace = await _context.Workspaces
            .FirstOrDefaultAsync(w => w.Id == workspaceId);
        
        if (workspace == null) return;

        // Ay kontrolü
        if (workspace.LastStopResetDate.Month != DateTime.UtcNow.Month ||
            workspace.LastStopResetDate.Year != DateTime.UtcNow.Year)
        {
            workspace.ResetMonthlyUsage();
        }

        // Önceki ek durak sayısını hesapla
        var limits = GetPlanLimits(workspace.PlanType);
        var previousExtraStops = Math.Max(0, workspace.CurrentMonthStops - limits.IncludedMonthlyStops);
        
        // Durak sayacını artır
        workspace.IncrementStopUsage(count);
        
        // Yeni ek durak sayısını hesapla
        var newExtraStops = Math.Max(0, workspace.CurrentMonthStops - limits.IncludedMonthlyStops);

        // MONITORING: Plan limit tracking and overage alerts
        var remainingStops = limits.IncludedMonthlyStops - workspace.CurrentMonthStops;

        if (workspace.CurrentMonthStops >= limits.IncludedMonthlyStops)
        {
            _logger.LogWarning(
                "⚠️ PLAN LIMIT EXCEEDED - Workspace {WorkspaceId} ({WorkspaceName}) exceeded stop plan limit. Current: {Current}, Limit: {Limit}, Overage: {Overage}",
                workspaceId, workspace.Name, workspace.CurrentMonthStops, limits.IncludedMonthlyStops, newExtraStops);
        }
        else if (remainingStops <= limits.IncludedMonthlyStops * 0.1) // %90 kullanıldı
        {
            _logger.LogWarning(
                "⚠️ PLAN LIMIT WARNING - Workspace {WorkspaceId} ({WorkspaceName}) approaching stop limit. Current: {Current}/{Limit}, Remaining: {Remaining}",
                workspaceId, workspace.Name, workspace.CurrentMonthStops, limits.IncludedMonthlyStops, remainingStops);
        }

        // Sadece yeni ek duraklar için ücret ekle
        if (newExtraStops > previousExtraStops)
        {
            var additionalStopsCount = newExtraStops - previousExtraStops;
            var additionalCharge = additionalStopsCount * limits.AdditionalStopPrice;
            workspace.AddAdditionalCharge(additionalCharge);

            _logger.LogWarning(
                "💰 OVERAGE CHARGE - Workspace {WorkspaceId} ({WorkspaceName}) exceeded stop limit. New extra stops: {ExtraStops}, Charge added: ₺{Charge}",
                workspaceId, workspace.Name, additionalStopsCount, additionalCharge);
        }

        await _context.SaveChangesAsync();
    }

    public async Task<bool> CanUseTimeWindows(int workspaceId)
    {
        var workspace = await _context.Workspaces
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == workspaceId);
        
        if (workspace == null) return false;
        
        return workspace.PlanType != PlanType.Starter;
    }

    public async Task<bool> CanUseCustomerSatisfactionReport(int workspaceId)
    {
        var workspace = await _context.Workspaces
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == workspaceId);
        
        if (workspace == null) return false;
        
        return workspace.PlanType != PlanType.Starter;
    }

    public async Task<bool> CanUseCustomReports(int workspaceId)
    {
        var workspace = await _context.Workspaces
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == workspaceId);
        
        if (workspace == null) return false;
        
        return workspace.PlanType == PlanType.Business;
    }

    public async Task<bool> CanSendWhatsApp(int workspaceId)
    {
        var workspace = await _context.Workspaces
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == workspaceId);
        
        if (workspace == null) return false;
        
        // Starter plan WhatsApp kullanamaz
        if (workspace.PlanType == PlanType.Starter) return false;
        
        // Trial kullanıcıları için sert limit kontrolü
        if (workspace.PlanType == PlanType.Trial)
        {
            // Ay kontrolü
            var currentMonth = DateTime.UtcNow;
            if (workspace.LastWhatsAppResetDate.Month != currentMonth.Month ||
                workspace.LastWhatsAppResetDate.Year != currentMonth.Year)
            {
                // Yeni ay başında limit sıfırlanmış sayılır
                return true;
            }

            var limits = GetPlanLimits(workspace.PlanType);
            
            // Trial WhatsApp limitine ulaşmışsa gönderimi engelle
            if (workspace.CurrentMonthWhatsAppMessages >= limits.IncludedWhatsAppMessages)
            {
                _logger.LogWarning(
                    "Trial workspace {WorkspaceId} reached WhatsApp limit: {Current}/{Limit}",
                    workspaceId, workspace.CurrentMonthWhatsAppMessages, limits.IncludedWhatsAppMessages);
                return false;
            }
        }
        
        // Diğer planlar kullanabilir
        return true;
    }

    public async Task RecordWhatsAppUsage(int workspaceId, int count = 1)
    {
        var workspace = await _context.Workspaces
            .FirstOrDefaultAsync(w => w.Id == workspaceId);
        
        if (workspace == null) return;

        // Ay kontrolü
        if (workspace.LastWhatsAppResetDate.Month != DateTime.UtcNow.Month ||
            workspace.LastWhatsAppResetDate.Year != DateTime.UtcNow.Year)
        {
            workspace.ResetMonthlyUsage();
        }

        // Önceki ek mesaj sayısını hesapla
        var limits = GetPlanLimits(workspace.PlanType);
        var previousExtraMessages = Math.Max(0, workspace.CurrentMonthWhatsAppMessages - limits.IncludedWhatsAppMessages);
        
        // WhatsApp sayacını artır
        workspace.IncrementWhatsAppUsage(count);
        
        // Yeni ek mesaj sayısını hesapla
        var newExtraMessages = Math.Max(0, workspace.CurrentMonthWhatsAppMessages - limits.IncludedWhatsAppMessages);

        // MONITORING: Plan limit tracking and overage alerts
        var remainingMessages = limits.IncludedWhatsAppMessages - workspace.CurrentMonthWhatsAppMessages;

        if (workspace.CurrentMonthWhatsAppMessages >= limits.IncludedWhatsAppMessages)
        {
            _logger.LogWarning(
                "⚠️ PLAN LIMIT EXCEEDED - Workspace {WorkspaceId} ({WorkspaceName}) exceeded WhatsApp plan limit. Current: {Current}, Limit: {Limit}, Overage: {Overage}",
                workspaceId, workspace.Name, workspace.CurrentMonthWhatsAppMessages, limits.IncludedWhatsAppMessages, newExtraMessages);
        }
        else if (remainingMessages <= limits.IncludedWhatsAppMessages * 0.1) // %90 kullanıldı
        {
            _logger.LogWarning(
                "⚠️ PLAN LIMIT WARNING - Workspace {WorkspaceId} ({WorkspaceName}) approaching WhatsApp limit. Current: {Current}/{Limit}, Remaining: {Remaining}",
                workspaceId, workspace.Name, workspace.CurrentMonthWhatsAppMessages, limits.IncludedWhatsAppMessages, remainingMessages);
        }

        // Sadece yeni ek mesajlar için ücret ekle
        if (newExtraMessages > previousExtraMessages)
        {
            var additionalMessagesCount = newExtraMessages - previousExtraMessages;
            var additionalCharge = additionalMessagesCount * limits.AdditionalWhatsAppPrice;
            workspace.AddAdditionalCharge(additionalCharge);

            _logger.LogWarning(
                "💰 OVERAGE CHARGE - Workspace {WorkspaceId} ({WorkspaceName}) - WhatsApp usage: {Current}/{Limit}. New extra messages: {Extra}, Charge added: ₺{Charge}",
                workspaceId, workspace.Name, workspace.CurrentMonthWhatsAppMessages, limits.IncludedWhatsAppMessages,
                additionalMessagesCount, additionalCharge);
        }

        await _context.SaveChangesAsync();
    }

    public async Task<WorkspaceUsageDto> GetCurrentUsage(int workspaceId)
    {
        var workspace = await _context.Workspaces
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == workspaceId);
        
        if (workspace == null) return null;

        var limits = GetPlanLimits(workspace.PlanType);
        
        // Ay kontrolü
        var currentMonth = DateTime.UtcNow;
        if (workspace.LastStopResetDate.Month != currentMonth.Month ||
            workspace.LastStopResetDate.Year != currentMonth.Year)
        {
            // Görüntüleme için sıfırlanmış değerler göster
            return new WorkspaceUsageDto
            {
                WorkspaceId = workspace.Id,
                WorkspaceName = workspace.Name,
                PlanType = workspace.PlanType,
                IncludedMonthlyStops = limits.IncludedMonthlyStops,
                CurrentMonthStops = 0,
                RemainingStops = limits.IncludedMonthlyStops,
                IncludedWhatsAppMessages = limits.IncludedWhatsAppMessages,
                CurrentMonthWhatsAppMessages = 0,
                RemainingWhatsAppMessages = limits.IncludedWhatsAppMessages,
                CurrentMonthAdditionalCharges = 0,
                EstimatedMonthlyTotal = limits.MonthlyPrice,
                LastResetDate = currentMonth,
                NextResetDate = new DateTime(currentMonth.Year, currentMonth.Month, 1).AddMonths(1)
            };
        }

        var remainingStops = Math.Max(0, limits.IncludedMonthlyStops - workspace.CurrentMonthStops);
        var remainingWhatsApp = Math.Max(0, limits.IncludedWhatsAppMessages - workspace.CurrentMonthWhatsAppMessages);

        return new WorkspaceUsageDto
        {
            WorkspaceId = workspace.Id,
            WorkspaceName = workspace.Name,
            PlanType = workspace.PlanType,
            IncludedMonthlyStops = limits.IncludedMonthlyStops,
            CurrentMonthStops = workspace.CurrentMonthStops,
            RemainingStops = remainingStops,
            IncludedWhatsAppMessages = limits.IncludedWhatsAppMessages,
            CurrentMonthWhatsAppMessages = workspace.CurrentMonthWhatsAppMessages,
            RemainingWhatsAppMessages = remainingWhatsApp,
            CurrentMonthAdditionalCharges = workspace.CurrentMonthAdditionalCharges,
            EstimatedMonthlyTotal = limits.MonthlyPrice + workspace.CurrentMonthAdditionalCharges,
            LastResetDate = workspace.LastStopResetDate,
            NextResetDate = new DateTime(currentMonth.Year, currentMonth.Month, 1).AddMonths(1)
        };
    }

    public async Task ResetMonthlyUsage(int workspaceId)
    {
        var workspace = await _context.Workspaces
            .FirstOrDefaultAsync(w => w.Id == workspaceId);
        
        if (workspace == null) return;

        workspace.ResetMonthlyUsage();
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Monthly usage reset for workspace {WorkspaceId}", workspaceId);
    }

    public int GetProofArchiveDays(PlanType planType)
    {
        var limits = GetPlanLimits(planType);
        return limits.ProofArchiveDays;
    }

    public async Task<bool> CheckTrialExpiry(int workspaceId)
    {
        var workspace = await _context.Workspaces
            .FirstOrDefaultAsync(w => w.Id == workspaceId);

        if (workspace == null) return false;

        // Trial planında değilse kontrol etme
        if (workspace.PlanType != PlanType.Trial) return false;

        // Trial süresi dolmuş mu?
        if (workspace.TrialEndDate.HasValue && DateTime.UtcNow > workspace.TrialEndDate.Value)
        {
            _logger.LogWarning("Trial expired for workspace {WorkspaceId} on {ExpiryDate}", 
                workspaceId, workspace.TrialEndDate.Value);
            return true;
        }

        return false;
    }

    public async Task<bool> IsTrialPlan(int workspaceId)
    {
        var workspace = await _context.Workspaces
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == workspaceId);

        return workspace?.PlanType == PlanType.Trial;
    }

    public async Task<bool> CanUseFeatureInTrial(int workspaceId, string featureName)
    {
        var isTrialPlan = await IsTrialPlan(workspaceId);
        if (!isTrialPlan) return true; // Trial değilse özellik kontrolü yapmıyoruz

        var isExpired = await CheckTrialExpiry(workspaceId);
        if (isExpired) return false; // Trial süresi dolmuşsa hiçbir özellik kullanılamaz

        // Trial'da kullanılabilir özellikler
        var allowedFeatures = new[]
        {
            "route_creation",
            "driver_management", 
            "vehicle_management",
            "customer_management",
            "basic_reports",
            "whatsapp_limited" // 25 mesaj
        };

        return allowedFeatures.Contains(featureName.ToLower());
    }
}