// src/Monolith.WebAPI/Applications/Queries/Settings/GetNotificationSettingsQuery.cs

using Microsoft.EntityFrameworkCore;
using Monolith.WebAPI.Applications.Commands;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Data.Workspace.Enums;
using Monolith.WebAPI.Responses.Settings;
using Monolith.WebAPI.Services.Members;

namespace Monolith.WebAPI.Applications.Queries.Settings;

public class GetNotificationSettingsQuery : BaseAuthenticatedCommand<NotificationSettingsResponse>
{
}

public class GetNotificationSettingsQueryHandler : BaseAuthenticatedCommandHandler<GetNotificationSettingsQuery, NotificationSettingsResponse>
{
    private readonly AppDbContext _context;

    public GetNotificationSettingsQueryHandler(IUserService userService, AppDbContext context) 
        : base(userService)
    {
        _context = context;
    }

    protected override async Task<NotificationSettingsResponse> HandleCommand(GetNotificationSettingsQuery request, CancellationToken cancellationToken)
    {
        var workspace = await _context.Workspaces
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == User.WorkspaceId, cancellationToken);

        if (workspace == null)
            throw new Exception("Workspace not found");

        // WhatsApp Settings'i oluştur - direkt Workspace'den oku
        var whatsAppSettings = new WhatsAppSettingsResponse
        {
            Mode = workspace.WhatsAppMode.ToString().ToLowerInvariant(),
            Enabled = workspace.WhatsAppMode != WhatsAppMode.Disabled,
            EnableWhatsAppForJourneyStart = workspace.WhatsAppNotifyJourneyStart,
            EnableWhatsAppForCheckIn = workspace.WhatsAppNotifyCheckIn,
            EnableWhatsAppForCompletion = workspace.WhatsAppNotifyCompletion,
            EnableWhatsAppForFailure = workspace.WhatsAppNotifyFailure
        };

        return new NotificationSettingsResponse
        {
            EmailNotifications = true,
            SmsNotifications = false,
            NotificationEmail = workspace.Email ?? "bildirim@rotaapp.com",
            NotificationPhone = workspace.PhoneNumber ?? "0532 123 45 67",
            WhatsAppSettings = whatsAppSettings,
            Events = new NotificationEvents
            {
                RouteCompleted = true,
                DeliveryFailed = true,
                DriverDelayed = true,
                NewCustomer = false,
                DailyReport = true
            }
        };
    }
}