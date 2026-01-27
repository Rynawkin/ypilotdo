// src/Monolith.WebAPI/Applications/Commands/Settings/UpdateDelayAlertSettingsCommand.cs

using MediatR;
using Microsoft.EntityFrameworkCore;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Data.Workspace;
using Monolith.WebAPI.Services.Members;

namespace Monolith.WebAPI.Applications.Commands.Settings;

/// <summary>
/// Gecikme uyarı ayarlarını güncelleme komutu
/// </summary>
public class UpdateDelayAlertSettingsCommand : BaseAuthenticatedCommand<Unit>
{
    public override bool RequiresAdmin => true; // Sadece admin veya dispatcher güncelleyebilir
    public override bool RequiresDispatcher => true;

    /// <summary>
    /// Gecikme uyarılarını aktif et/devre dışı bırak
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Kaç saat gecikme sonrası uyarı gönderilsin
    /// </summary>
    public int ThresholdHours { get; set; }

    /// <summary>
    /// Gecikme raporlarının gönderileceği email adresleri (virgülle ayrılmış)
    /// </summary>
    public string AlertEmails { get; set; }
}

public class UpdateDelayAlertSettingsCommandHandler : BaseAuthenticatedCommandHandler<UpdateDelayAlertSettingsCommand, Unit>
{
    private readonly AppDbContext _context;

    public UpdateDelayAlertSettingsCommandHandler(IUserService userService, AppDbContext context)
        : base(userService)
    {
        _context = context;
    }

    protected override async Task<Unit> HandleCommand(UpdateDelayAlertSettingsCommand request, CancellationToken cancellationToken)
    {
        var workspace = await _context.Workspaces
            .FirstOrDefaultAsync(w => w.Id == User.WorkspaceId, cancellationToken);

        if (workspace == null)
            throw new Exception("Workspace not found");

        // Eğer Settings null ise yeni oluştur
        if (workspace.Settings == null)
        {
            workspace.Settings = new WorkspaceSettings();
        }

        // DelayAlertSettings'i güncelle veya oluştur
        if (workspace.Settings.DelayAlertSettings == null)
        {
            workspace.Settings.DelayAlertSettings = new DelayAlertSettings();
        }

        workspace.Settings.DelayAlertSettings.Enabled = request.Enabled;
        workspace.Settings.DelayAlertSettings.ThresholdHours = request.ThresholdHours;
        workspace.Settings.DelayAlertSettings.AlertEmails = request.AlertEmails;

        workspace.UpdatedAt = DateTime.UtcNow;

        // EF Core JSON column'u otomatik detect etmeyebilir, manuel olarak mark edelim
        _context.Entry(workspace).Property(w => w.Settings).IsModified = true;

        await _context.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
