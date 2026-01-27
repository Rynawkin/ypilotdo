// src/Monolith.WebAPI/Services/Subscription/ISubscriptionService.cs

using Monolith.WebAPI.Data.Workspace;

namespace Monolith.WebAPI.Services.Subscription;

public interface ISubscriptionService
{
    PlanLimits GetPlanLimits(PlanType planType);
    
    // int workspaceId olarak değiştir
    Task<bool> CanAddStop(int workspaceId);
    Task RecordStopUsage(int workspaceId, int count = 1);
    Task<bool> CanUseTimeWindows(int workspaceId);
    Task<bool> CanUseCustomerSatisfactionReport(int workspaceId);
    Task<bool> CanUseCustomReports(int workspaceId);
    Task<bool> CanSendWhatsApp(int workspaceId);
    Task RecordWhatsAppUsage(int workspaceId, int count = 1);
    Task<WorkspaceUsageDto> GetCurrentUsage(int workspaceId);
    Task ResetMonthlyUsage(int workspaceId);
    int GetProofArchiveDays(PlanType planType);
}

public class PlanLimits
{
    public PlanType PlanType { get; set; }
    public decimal MonthlyPrice { get; set; }
    
    // Temel Limitler
    public int? MaxDrivers { get; set; }
    public int? MaxVehicles { get; set; }
    public int? MaxCustomers { get; set; }
    public int? MaxUsers { get; set; }
    
    // Durak Limitleri
    public int IncludedMonthlyStops { get; set; }
    public decimal AdditionalStopPrice { get; set; }
    
    // WhatsApp Limitleri
    public bool HasCustomerWhatsAppNotifications { get; set; }
    public int IncludedWhatsAppMessages { get; set; }
    public decimal AdditionalWhatsAppPrice { get; set; }
    
    // Özellik Flags
    public bool HasTimeWindows { get; set; }
    public bool HasCustomerSatisfactionReport { get; set; }
    public bool HasRouteTemplates { get; set; }
    public bool HasCustomReports { get; set; }
    
    // Arşiv Süresi (gün)
    public int ProofArchiveDays { get; set; }
}

public class WorkspaceUsageDto
{
    public int WorkspaceId { get; set; }
    public string WorkspaceName { get; set; }
    public PlanType PlanType { get; set; }
    
    // Limitler
    public int IncludedMonthlyStops { get; set; }
    public int CurrentMonthStops { get; set; }
    public int RemainingStops { get; set; }
    
    // WhatsApp
    public int IncludedWhatsAppMessages { get; set; }
    public int CurrentMonthWhatsAppMessages { get; set; }
    public int RemainingWhatsAppMessages { get; set; }
    
    // Maliyetler
    public decimal CurrentMonthAdditionalCharges { get; set; }
    public decimal EstimatedMonthlyTotal { get; set; }
    
    // Tarihler
    public DateTime LastResetDate { get; set; }
    public DateTime NextResetDate { get; set; }
}