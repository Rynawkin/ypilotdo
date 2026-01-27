// src/Monolith.WebAPI/Applications/Queries/Settings/GetDeliverySettingsQuery.cs

using Microsoft.EntityFrameworkCore;
using Monolith.WebAPI.Applications.Commands;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Responses.Settings;
using Monolith.WebAPI.Services.Members;
using System.Text.Json;

namespace Monolith.WebAPI.Applications.Queries.Settings;

public class GetDeliverySettingsQuery : BaseAuthenticatedCommand<DeliverySettingsResponse>
{
}

public class GetDeliverySettingsQueryHandler : BaseAuthenticatedCommandHandler<GetDeliverySettingsQuery, DeliverySettingsResponse>
{
    private readonly AppDbContext _context;

    public GetDeliverySettingsQueryHandler(IUserService userService, AppDbContext context) 
        : base(userService)
    {
        _context = context;
    }

    protected override async Task<DeliverySettingsResponse> HandleCommand(GetDeliverySettingsQuery request, CancellationToken cancellationToken)
    {
        var workspace = await _context.Workspaces
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == User.WorkspaceId, cancellationToken);

        if (workspace == null)
            throw new Exception("Workspace not found");

        // Settings direkt olarak WorkspaceSettings tipinde
        var settings = workspace.Settings ?? new Data.Workspace.WorkspaceSettings();

        return new DeliverySettingsResponse
        {
            DefaultServiceTime = (int)workspace.DefaultServiceTime.TotalMinutes,
            DefaultSignatureRequired = settings.DefaultSignatureRequired,
            DefaultPhotoRequired = settings.DefaultPhotoRequired,
            MaxDeliveriesPerRoute = settings.MaxDeliveriesPerRoute,
            AutoOptimize = settings.AutoOptimize,
            TrafficConsideration = settings.TrafficConsideration,
            CostPerKm = workspace.CostPerKm,
            CostPerHour = workspace.CostPerHour,
            WorkingHours = GetWorkingHours(workspace),
            PrioritySettings = GetPrioritySettings(workspace)
        };
    }

    private Dictionary<string, Responses.Settings.WorkingHours> GetWorkingHours(Data.Workspace.Workspace workspace)
    {
        // JSON'dan oku, yoksa default döndür
        if (!string.IsNullOrEmpty(workspace.WorkingHours))
        {
            try
            {
                return JsonSerializer.Deserialize<Dictionary<string, Responses.Settings.WorkingHours>>(workspace.WorkingHours);
            }
            catch
            {
                // JSON parse hatası durumunda default döndür
                return GetDefaultWorkingHours();
            }
        }
        
        return GetDefaultWorkingHours();
    }

    private Responses.Settings.PrioritySettings GetPrioritySettings(Data.Workspace.Workspace workspace)
    {
        // JSON'dan oku, yoksa default döndür
        if (!string.IsNullOrEmpty(workspace.PrioritySettings))
        {
            try
            {
                return JsonSerializer.Deserialize<Responses.Settings.PrioritySettings>(workspace.PrioritySettings);
            }
            catch
            {
                // JSON parse hatası durumunda default döndür
                return GetDefaultPrioritySettings();
            }
        }
        
        return GetDefaultPrioritySettings();
    }

    private Dictionary<string, Responses.Settings.WorkingHours> GetDefaultWorkingHours()
    {
        return new Dictionary<string, Responses.Settings.WorkingHours>
        {
            ["monday"] = new() { Start = "08:00", End = "18:00", Enabled = true },
            ["tuesday"] = new() { Start = "08:00", End = "18:00", Enabled = true },
            ["wednesday"] = new() { Start = "08:00", End = "18:00", Enabled = true },
            ["thursday"] = new() { Start = "08:00", End = "18:00", Enabled = true },
            ["friday"] = new() { Start = "08:00", End = "18:00", Enabled = true },
            ["saturday"] = new() { Start = "09:00", End = "14:00", Enabled = true },
            ["sunday"] = new() { Start = "09:00", End = "14:00", Enabled = false }
        };
    }

    private Responses.Settings.PrioritySettings GetDefaultPrioritySettings()
    {
        return new Responses.Settings.PrioritySettings
        {
            High = new PriorityLevel { Color = "#EF4444", MaxDelay = 30 },
            Normal = new PriorityLevel { Color = "#F59E0B", MaxDelay = 60 },
            Low = new PriorityLevel { Color = "#10B981", MaxDelay = 120 }
        };
    }
}