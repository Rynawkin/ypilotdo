using Microsoft.EntityFrameworkCore;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Data.Workspace;

namespace Monolith.WebAPI.Services.Payment;

public interface ITrialService
{
    Task<bool> CanStartTrialAsync(int workspaceId);
    Task<TrialResult> StartTrialAsync(int workspaceId);
    Task<TrialStatus> GetTrialStatusAsync(int workspaceId);
    Task<bool> IsTrialExpiredAsync(int workspaceId);
    Task<int> GetRemainingTrialDaysAsync(int workspaceId);
    Task<bool> IsWithinTrialLimitsAsync(int workspaceId, string limitType, int currentUsage);
}

public class TrialService : ITrialService
{
    private readonly AppDbContext _context;
    private readonly ILogger<TrialService> _logger;
    private readonly IConfiguration _configuration;

    // Trial Limits - appsettings.json'dan okunacak
    private readonly int _trialDays;
    private readonly int _maxStops;
    private readonly int _maxWhatsAppMessages;
    private readonly int _maxDrivers;
    private readonly int _maxVehicles;

    public TrialService(AppDbContext context, ILogger<TrialService> logger, IConfiguration configuration)
    {
        _context = context;
        _logger = logger;
        _configuration = configuration;

        _trialDays = _configuration.GetValue<int>("Payment:TrialDays", 14);
        _maxStops = _configuration.GetValue<int>("Payment:TrialLimits:MaxStops", 100);
        _maxWhatsAppMessages = _configuration.GetValue<int>("Payment:TrialLimits:MaxWhatsAppMessages", 25);
        _maxDrivers = _configuration.GetValue<int>("Payment:TrialLimits:MaxDrivers", 2);
        _maxVehicles = _configuration.GetValue<int>("Payment:TrialLimits:MaxVehicles", 1);
    }

    public async Task<bool> CanStartTrialAsync(int workspaceId)
    {
        var workspace = await _context.Workspaces
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == workspaceId);

        if (workspace == null) return false;

        // Trial daha önce kullanılmışsa başlatılamaz
        if (workspace.IsTrialUsed) return false;

        // Mevcut plan Trial değilse başlatılamaz (çünkü zaten ücretli plan aktif)
        if (workspace.PlanType != PlanType.Trial) return false;

        return true;
    }

    public async Task<TrialResult> StartTrialAsync(int workspaceId)
    {
        var workspace = await _context.Workspaces
            .FirstOrDefaultAsync(w => w.Id == workspaceId);

        if (workspace == null)
        {
            return new TrialResult
            {
                IsSuccess = false,
                ErrorMessage = "Workspace not found"
            };
        }

        if (!await CanStartTrialAsync(workspaceId))
        {
            return new TrialResult
            {
                IsSuccess = false,
                ErrorMessage = "Trial cannot be started. Either already used or paid plan is active."
            };
        }

        workspace.PlanType = PlanType.Trial;
        workspace.TrialStartDate = DateTime.UtcNow;
        workspace.TrialEndDate = DateTime.UtcNow.AddDays(_trialDays);
        workspace.IsTrialUsed = true;
        workspace.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Trial started for workspace {WorkspaceId}, expires on {ExpiryDate}", 
            workspaceId, workspace.TrialEndDate);

        return new TrialResult
        {
            IsSuccess = true,
            TrialStartDate = workspace.TrialStartDate.Value,
            TrialEndDate = workspace.TrialEndDate.Value,
            RemainingDays = _trialDays
        };
    }

    public async Task<TrialStatus> GetTrialStatusAsync(int workspaceId)
    {
        var workspace = await _context.Workspaces
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == workspaceId);

        if (workspace == null)
        {
            return new TrialStatus { IsActive = false };
        }

        if (workspace.PlanType != PlanType.Trial || !workspace.TrialStartDate.HasValue)
        {
            return new TrialStatus 
            { 
                IsActive = false,
                IsExpired = workspace.IsTrialUsed
            };
        }

        var now = DateTime.UtcNow;
        var isExpired = workspace.TrialEndDate.HasValue && now > workspace.TrialEndDate.Value;
        var remainingDays = workspace.TrialEndDate.HasValue 
            ? Math.Max(0, (int)(workspace.TrialEndDate.Value - now).TotalDays)
            : 0;

        return new TrialStatus
        {
            IsActive = !isExpired,
            IsExpired = isExpired,
            StartDate = workspace.TrialStartDate.Value,
            EndDate = workspace.TrialEndDate,
            RemainingDays = remainingDays,
            Limits = new TrialLimits
            {
                MaxStops = _maxStops,
                MaxWhatsAppMessages = _maxWhatsAppMessages,
                MaxDrivers = _maxDrivers,
                MaxVehicles = _maxVehicles,
                CurrentStops = workspace.CurrentMonthStops,
                CurrentWhatsAppMessages = workspace.CurrentMonthWhatsAppMessages
            }
        };
    }

    public async Task<bool> IsTrialExpiredAsync(int workspaceId)
    {
        var status = await GetTrialStatusAsync(workspaceId);
        return status.IsExpired;
    }

    public async Task<int> GetRemainingTrialDaysAsync(int workspaceId)
    {
        var status = await GetTrialStatusAsync(workspaceId);
        return status.RemainingDays;
    }

    public async Task<bool> IsWithinTrialLimitsAsync(int workspaceId, string limitType, int currentUsage)
    {
        var workspace = await _context.Workspaces
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == workspaceId);

        if (workspace?.PlanType != PlanType.Trial) return true; // Trial değilse limit yok

        return limitType.ToLower() switch
        {
            "stops" => currentUsage < _maxStops,
            "whatsapp" => currentUsage < _maxWhatsAppMessages,
            "drivers" => currentUsage < _maxDrivers,
            "vehicles" => currentUsage < _maxVehicles,
            _ => true
        };
    }
}

public class TrialResult
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime TrialStartDate { get; set; }
    public DateTime TrialEndDate { get; set; }
    public int RemainingDays { get; set; }
}

public class TrialStatus
{
    public bool IsActive { get; set; }
    public bool IsExpired { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int RemainingDays { get; set; }
    public TrialLimits? Limits { get; set; }
}

public class TrialLimits
{
    public int MaxStops { get; set; }
    public int MaxWhatsAppMessages { get; set; }
    public int MaxDrivers { get; set; }
    public int MaxVehicles { get; set; }
    public int CurrentStops { get; set; }
    public int CurrentWhatsAppMessages { get; set; }
}