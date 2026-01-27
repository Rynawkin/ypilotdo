// src/Monolith.WebAPI/Applications/Queries/Settings/GetDelayAlertSettingsQuery.cs

using Microsoft.EntityFrameworkCore;
using Monolith.WebAPI.Applications.Commands;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Data.Workspace;
using Monolith.WebAPI.Services.Members;

namespace Monolith.WebAPI.Applications.Queries.Settings;

/// <summary>
/// Gecikme uyarı ayarlarını getir
/// </summary>
public class GetDelayAlertSettingsQuery : BaseAuthenticatedCommand<DelayAlertSettingsResponse>
{
    public override bool RequiresDispatcher => true;
}

public class DelayAlertSettingsResponse
{
    public bool Enabled { get; set; }
    public int ThresholdHours { get; set; }
    public string AlertEmails { get; set; }
}

public class GetDelayAlertSettingsQueryHandler : BaseAuthenticatedCommandHandler<GetDelayAlertSettingsQuery, DelayAlertSettingsResponse>
{
    private readonly AppDbContext _context;

    public GetDelayAlertSettingsQueryHandler(IUserService userService, AppDbContext context)
        : base(userService)
    {
        _context = context;
    }

    protected override async Task<DelayAlertSettingsResponse> HandleCommand(GetDelayAlertSettingsQuery request, CancellationToken cancellationToken)
    {
        var workspace = await _context.Workspaces
            .FirstOrDefaultAsync(w => w.Id == User.WorkspaceId, cancellationToken);

        if (workspace == null)
            throw new Exception("Workspace not found");

        var settings = workspace.Settings?.DelayAlertSettings;

        return new DelayAlertSettingsResponse
        {
            Enabled = settings?.Enabled ?? false,
            ThresholdHours = settings?.ThresholdHours ?? 1,
            AlertEmails = settings?.AlertEmails ?? string.Empty
        };
    }
}
