// src/Monolith.WebAPI/Services/BackgroundJobs/MaintenanceReminderJob.cs

using Microsoft.EntityFrameworkCore;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Data.Workspace;
using Monolith.WebAPI.Services.Email;

namespace Monolith.WebAPI.Services.BackgroundJobs;

public class MaintenanceReminderJob : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MaintenanceReminderJob> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromHours(24); // Her 24 saatte bir çalış

    public MaintenanceReminderJob(IServiceProvider serviceProvider, ILogger<MaintenanceReminderJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MaintenanceReminderJob started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckAndSendReminders(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while processing maintenance reminders");
            }

            // Wait for next check (24 hours)
            await Task.Delay(_checkInterval, stoppingToken);
        }
    }

    private async Task CheckAndSendReminders(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

        var today = DateTime.UtcNow.Date;

        // Get active reminders
        var reminders = await dbContext.MaintenanceReminders
            .Include(r => r.Vehicle)
                .ThenInclude(v => v!.Workspace)
            .Include(r => r.Maintenance)
            .Where(r => r.IsActive)
            .ToListAsync(cancellationToken);

        _logger.LogInformation($"Found {reminders.Count} active reminders to check");

        foreach (var reminder in reminders)
        {
            if (reminder.Vehicle == null || reminder.Vehicle.Workspace == null)
                continue;

            bool shouldSendReminder = false;
            string reminderReason = "";

            // Check date-based reminder
            if (reminder.NextMaintenanceDate.HasValue && reminder.ReminderDays.HasValue)
            {
                var daysUntilMaintenance = (reminder.NextMaintenanceDate.Value.Date - today).Days;
                if (daysUntilMaintenance == reminder.ReminderDays.Value)
                {
                    shouldSendReminder = true;
                    reminderReason = "tarih";
                }
            }

            // Check km-based reminder
            if (reminder.NextMaintenanceKm.HasValue && reminder.ReminderKm.HasValue && reminder.Vehicle.CurrentKm.HasValue)
            {
                var kmUntilMaintenance = reminder.NextMaintenanceKm.Value - reminder.Vehicle.CurrentKm.Value;
                if (kmUntilMaintenance <= reminder.ReminderKm.Value && kmUntilMaintenance > 0)
                {
                    shouldSendReminder = true;
                    reminderReason = shouldSendReminder ? "tarih ve kilometre" : "kilometre";
                }
            }

            // Send reminder if either condition is met
            if (shouldSendReminder)
            {
                await SendReminderEmail(reminder, dbContext, emailService, cancellationToken);

                // Deactivate reminder after sending
                reminder.IsActive = false;
                reminder.SentAt = DateTime.UtcNow;
                await dbContext.SaveChangesAsync(cancellationToken);

                _logger.LogInformation($"Sent {reminderReason} based reminder for vehicle {reminder.Vehicle.PlateNumber}, deactivated reminder {reminder.Id}");
            }
        }
    }

    private async Task SendReminderEmail(
        MaintenanceReminder reminder,
        AppDbContext dbContext,
        IEmailService emailService,
        CancellationToken cancellationToken)
    {
        if (reminder.Vehicle == null || reminder.Vehicle.Workspace == null)
            return;

        var workspaceId = reminder.Vehicle.WorkspaceId;

        // Get workspace admin and managers via UserRoles
        var adminManagerRoleIds = await dbContext.Roles
            .Where(r => r.Name == "admin" || r.Name == "manager")
            .Select(r => r.Id)
            .ToListAsync(cancellationToken);

        var recipients = await dbContext.Users
            .Where(u => u.WorkspaceId == workspaceId
                     && dbContext.UserRoles.Any(ur => ur.UserId == u.Id && adminManagerRoleIds.Contains(ur.RoleId)))
            .Select(u => new { u.Email, u.FullName })
            .ToListAsync(cancellationToken);

        // Get driver if assigned to vehicle (assuming there's a driver assignment in the future)
        // For now, we'll skip driver as Vehicle doesn't have DriverId yet

        var maintenanceTypeName = GetMaintenanceTypeLabel(reminder.Maintenance?.Type);

        foreach (var recipient in recipients)
        {
            try
            {
                await emailService.SendMaintenanceReminderEmail(
                    to: recipient.Email,
                    recipientName: recipient.FullName ?? "Kullanıcı",
                    vehiclePlate: reminder.Vehicle.PlateNumber,
                    vehicleBrand: $"{reminder.Vehicle.Brand} {reminder.Vehicle.Model}",
                    maintenanceType: maintenanceTypeName,
                    maintenanceDate: reminder.NextMaintenanceDate ?? DateTime.UtcNow,
                    daysRemaining: reminder.ReminderDays ?? 0
                );

                _logger.LogInformation($"Sent maintenance reminder email to {recipient.Email}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send maintenance reminder email to {recipient.Email}");
            }
        }
    }

    private string GetMaintenanceTypeLabel(string? type)
    {
        return type switch
        {
            "routine" => "Rutin Bakım",
            "repair" => "Tamir",
            "inspection" => "Muayene",
            "tire_change" => "Lastik Değişimi",
            "oil_change" => "Yağ Değişimi",
            "other" => "Diğer",
            _ => "Bakım"
        };
    }
}
