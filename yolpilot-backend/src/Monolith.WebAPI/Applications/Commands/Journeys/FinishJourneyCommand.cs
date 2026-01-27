using System.Text.Json.Serialization;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Data.Journeys;
using Monolith.WebAPI.Infrastructure;
using Monolith.WebAPI.Responses.Journeys;
using Monolith.WebAPI.Services.Members;
using Monolith.WebAPI.Services.Email;

namespace Monolith.WebAPI.Applications.Commands.Journeys;

public class FinishJourneyCommand : BaseAuthenticatedCommand<JourneyResponse>
{
    [JsonIgnore] public override bool RequiresDriver => true; // Driver ve üstü yapabilir
    [JsonIgnore] public override bool RequiresWorkspaceAccess => true;
    public int JourneyId { get; set; }
}

public class FinishJourneyCommandValidator : AbstractValidator<FinishJourneyCommand>
{
    public FinishJourneyCommandValidator()
    {
        RuleFor(x => x.JourneyId).GreaterThan(0);
    }
}

public class FinishJourneyCommandHandler(AppDbContext context, IUserService userService, IEmailService emailService)
    : BaseAuthenticatedCommandHandler<FinishJourneyCommand, JourneyResponse>(userService)
{
    protected override async Task<JourneyResponse> HandleCommand(FinishJourneyCommand request, CancellationToken cancellationToken)
    {
        var journey = await context.Journeys
            .Include(x => x.Route)
            .Include(x => x.Driver)
            .Include(x => x.Stops) // ✅ Stops'ları include et
                .ThenInclude(s => s.RouteStop) // ✅ RouteStop'u include et
                    .ThenInclude(rs => rs.Customer) // ✅ Customer'ı include et (LastDeliveryDate güncellemesi için)
            .FirstOrDefaultAsync(x => x.Id == request.JourneyId && x.Route.WorkspaceId == User.WorkspaceId, cancellationToken);

        if (journey == null)
            throw new ApiException("Journey not found.", 404);

        if (journey.FinishedAt.HasValue)
            throw new ApiException("Journey is already finished.", 400);

        // ✅ YENİ VALİDASYON: Başarısız duraklar olsa bile seferin tamamlanmasına izin ver
        // Sadece Pending veya InProgress duraklar varsa engelle
        var hasIncompletStops = journey.Stops != null && journey.Stops
            .Any(s => s.Status == JourneyStopStatus.Pending || 
                     s.Status == JourneyStopStatus.InProgress);

        if (hasIncompletStops)
        {
            // Hangi durakların tamamlanmadığını belirt
            var incompletStops = journey.Stops
                .Where(s => s.Status == JourneyStopStatus.Pending || 
                           s.Status == JourneyStopStatus.InProgress)
                .Select(s => $"Durak #{s.Order}")
                .ToList();
            
            var stopList = string.Join(", ", incompletStops);
            throw new ApiException($"Sefer tamamlanamaz. Tamamlanmamış veya devam eden duraklar var: {stopList}", 400);
        }

        // ✅ İstatistik bilgileri (opsiyonel)
        if (journey.Stops != null)
        {
            var completedCount = journey.Stops.Count(s => s.Status == JourneyStopStatus.Completed);
            var failedCount = journey.Stops.Count(s => s.Status == JourneyStopStatus.Failed);
            var skippedCount = journey.Stops.Count(s => s.Status == JourneyStopStatus.Skipped);
            
            // Bu bilgileri log'layabilir veya journey'ye ekleyebilirsiniz
            // Örnek: journey.SetCompletionStats(completedCount, failedCount, skippedCount);
            
            // Başarı oranı hesaplama
            var totalStops = journey.Stops.Count;
            var successRate = totalStops > 0 ? (completedCount * 100.0 / totalStops) : 0;
            
            // TODO: Bu istatistikleri Journey entity'sine veya ayrı bir tablo'ya kaydedebilirsiniz
        }

        // ✅ NOT: LastDeliveryDate artık her stop completed/failed olduğunda UpdateJourneyStopCommand'da güncelleniyor.
        // Journey finish olduğunda tüm stop'lar zaten completed/failed olduğu için burada tekrar güncellemeye gerek yok.

        // Use the Finish method on Journey entity
        journey.Finish();

        await context.SaveChangesAsync(cancellationToken);

        // ✅ YENİ: Gecikme kontrolü ve rapor gönderme
        await CheckAndSendDelayReportAsync(journey, cancellationToken);

        return new JourneyResponse(journey);
    }

    /// <summary>
    /// Seferin gecikmesini kontrol eder ve gerekirse yönetici emaillerine gecikme raporu gönderir
    /// </summary>
    private async System.Threading.Tasks.Task CheckAndSendDelayReportAsync(Journey journey, CancellationToken cancellationToken)
    {
        try
        {
            Console.WriteLine($"[DELAY_ALERT] Starting delay check for Journey #{journey.Id}");

            // Workspace bilgilerini al
            var workspace = await context.Workspaces
                .FirstOrDefaultAsync(w => w.Id == User.WorkspaceId, cancellationToken);

            Console.WriteLine($"[DELAY_ALERT] Workspace found: {workspace?.Name}, Settings: {workspace?.Settings != null}, DelayAlertSettings: {workspace?.Settings?.DelayAlertSettings != null}");

            if (workspace?.Settings?.DelayAlertSettings == null ||
                !workspace.Settings.DelayAlertSettings.Enabled)
            {
                // Gecikme uyarıları devre dışı
                Console.WriteLine($"[DELAY_ALERT] Delay alerts disabled or not configured. Enabled: {workspace?.Settings?.DelayAlertSettings?.Enabled}");
                return;
            }

            var delaySettings = workspace.Settings.DelayAlertSettings;
            var alertEmails = delaySettings.GetEmailList();

            Console.WriteLine($"[DELAY_ALERT] Alert emails count: {alertEmails.Count}, Emails: {string.Join(", ", alertEmails)}");

            if (!alertEmails.Any())
            {
                // Email adresi yok
                Console.WriteLine($"[DELAY_ALERT] No alert emails configured");
                return;
            }

            // Planlanan tamamlanma zamanını hesapla
            // Son stop'un estimated arrival time + service time kullanılır
            var lastStop = journey.Stops?.OrderBy(s => s.Order).LastOrDefault();
            if (lastStop == null || journey.Date == null || journey.FinishedAt == null)
            {
                // Bilgiler eksik, kontrol yapılamaz
                Console.WriteLine($"[DELAY_ALERT] Missing journey info - LastStop: {lastStop != null}, Date: {journey.Date != null}, FinishedAt: {journey.FinishedAt != null}");
                return;
            }

            // Kilometre bilgilerini hesapla
            decimal? plannedDistanceKm = null;
            if (journey.Route?.TotalDistance != null)
            {
                plannedDistanceKm = (decimal)journey.Route.TotalDistance.Value;
            }
            decimal? actualDistanceKm = null;
            if (journey.StartKm.HasValue && journey.EndKm.HasValue)
            {
                actualDistanceKm = journey.EndKm.Value - journey.StartKm.Value;
            }

            Console.WriteLine($"[DELAY_ALERT] Distance - Planned: {plannedDistanceKm?.ToString("F1") ?? "N/A"} km, Actual: {actualDistanceKm?.ToString("F1") ?? "N/A"} km");

            var plannedCompletionTime = journey.Date.Date.Add(lastStop.EstimatedArrivalTime);

            // Eğer EstimatedDepartureTime varsa onu kullan (service time dahil)
            if (lastStop.EstimatedDepartureTime.HasValue)
            {
                plannedCompletionTime = journey.Date.Date.Add(lastStop.EstimatedDepartureTime.Value);
            }

            var actualCompletionTime = journey.FinishedAt.Value;
            var delay = actualCompletionTime - plannedCompletionTime;

            Console.WriteLine($"[DELAY_ALERT] Planned: {plannedCompletionTime:yyyy-MM-dd HH:mm:ss}, Actual: {actualCompletionTime:yyyy-MM-dd HH:mm:ss}, Delay: {delay.TotalHours:F2} hours ({delay.TotalMinutes:F0} minutes), Threshold: {delaySettings.ThresholdHours} hours");

            // Gecikme threshold'unu kontrol et
            if (delay.TotalHours >= delaySettings.ThresholdHours)
            {
                Console.WriteLine($"[DELAY_ALERT] Delay threshold exceeded! Sending email to {alertEmails.Count} recipient(s)");

                // Gecikme var ve threshold'u aştı - email gönder
                await emailService.SendJourneyDelayReportAsync(
                    recipients: alertEmails,
                    journeyId: journey.Id,
                    journeyName: journey.Name,
                    driverName: journey.Driver?.Name ?? "Bilinmeyen Sürücü",
                    plannedCompletionTime: plannedCompletionTime,
                    actualCompletionTime: actualCompletionTime,
                    delayDuration: delay,
                    workspaceName: workspace.Name,
                    plannedDistanceKm: plannedDistanceKm,
                    actualDistanceKm: actualDistanceKm
                );

                Console.WriteLine($"[DELAY_ALERT] Email sent successfully");
            }
            else
            {
                Console.WriteLine($"[DELAY_ALERT] Delay {delay.TotalHours:F2}h is below threshold {delaySettings.ThresholdHours}h, no email sent");
            }
        }
        catch (Exception ex)
        {
            // Gecikme raporu gönderilirken hata olsa bile journey completion başarılı olmalı
            // Sadece log'la
            Console.WriteLine($"[DELAY_ALERT] Error checking/sending delay report for Journey #{journey.Id}: {ex.Message}");
            Console.WriteLine($"[DELAY_ALERT] Stack trace: {ex.StackTrace}");
        }
    }
}