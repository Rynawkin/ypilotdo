// src/Monolith.WebAPI/Applications/Commands/Settings/UpdateNotificationSettingsCommand.cs

using MediatR;
using Microsoft.EntityFrameworkCore;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Data.Workspace.Enums;
using Monolith.WebAPI.Responses.Settings;
using Monolith.WebAPI.Services.Members;

namespace Monolith.WebAPI.Applications.Commands.Settings;

public class UpdateNotificationSettingsCommand : BaseAuthenticatedCommand<Unit>
{
    public override bool RequiresDispatcher => true;
    
    public bool EmailNotifications { get; set; }
    public bool SmsNotifications { get; set; }
    public string NotificationEmail { get; set; }
    public string NotificationPhone { get; set; }
    public WhatsAppSettingsDto WhatsAppSettings { get; set; }
    public NotificationEvents Events { get; set; }
}

public class WhatsAppSettingsDto
{
    public string Mode { get; set; } = "disabled";
    public bool Enabled { get; set; }
    public bool EnableWhatsAppForJourneyStart { get; set; }
    public bool EnableWhatsAppForCheckIn { get; set; }
    public bool EnableWhatsAppForCompletion { get; set; }
    public bool EnableWhatsAppForFailure { get; set; }
}

public class UpdateNotificationSettingsCommandHandler : BaseAuthenticatedCommandHandler<UpdateNotificationSettingsCommand, Unit>
{
    private readonly AppDbContext _context;

    public UpdateNotificationSettingsCommandHandler(IUserService userService, AppDbContext context) 
        : base(userService)
    {
        _context = context;
    }

    protected override async Task<Unit> HandleCommand(UpdateNotificationSettingsCommand request, CancellationToken cancellationToken)
    {
        var workspace = await _context.Workspaces
            .FirstOrDefaultAsync(w => w.Id == User.WorkspaceId, cancellationToken);

        if (workspace == null)
            throw new Exception("Workspace not found");

        // Update notification-related fields using the new method
        workspace.UpdateNotificationSettings(
            request.NotificationEmail,
            request.NotificationPhone
        );

        // WhatsApp ayarlarını direkt Workspace'e kaydet
        if (request.WhatsAppSettings != null)
        {
            workspace.WhatsAppMode = request.WhatsAppSettings.Mode switch
            {
                "shared" => WhatsAppMode.Shared,
                "custom" => WhatsAppMode.Custom,
                _ => WhatsAppMode.Disabled
            };
            
            workspace.WhatsAppNotifyJourneyStart = request.WhatsAppSettings.EnableWhatsAppForJourneyStart;
            workspace.WhatsAppNotifyCheckIn = request.WhatsAppSettings.EnableWhatsAppForCheckIn;
            workspace.WhatsAppNotifyCompletion = request.WhatsAppSettings.EnableWhatsAppForCompletion;
            workspace.WhatsAppNotifyFailure = request.WhatsAppSettings.EnableWhatsAppForFailure;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}