using System.Text.Json.Serialization;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Data.Journeys;
using Monolith.WebAPI.Infrastructure;
using Monolith.WebAPI.Services.Members;

namespace Monolith.WebAPI.Applications.Commands.Journeys;

public class BulkCancelJourneysCommand : BaseAuthenticatedCommand<BulkOperationResult>
{
    [JsonIgnore] public override bool RequiresDispatcher => true; // Dispatcher ve üstü yapabilir
    [JsonIgnore] public override bool RequiresWorkspaceAccess => true;
    public List<int> JourneyIds { get; set; } = new();
    public string? Reason { get; set; }
}

public class BulkCancelJourneysCommandValidator : AbstractValidator<BulkCancelJourneysCommand>
{
    public BulkCancelJourneysCommandValidator()
    {
        RuleFor(x => x.JourneyIds)
            .NotEmpty().WithMessage("En az bir sefer seçmelisiniz")
            .Must(x => x.Count <= 100).WithMessage("Tek seferde maksimum 100 sefer iptal edilebilir");
        
        RuleForEach(x => x.JourneyIds)
            .GreaterThan(0).WithMessage("Geçersiz sefer ID");
    }
}

public class BulkCancelJourneysCommandHandler(AppDbContext context, IUserService userService)
    : BaseAuthenticatedCommandHandler<BulkCancelJourneysCommand, BulkOperationResult>(userService)
{
    protected override async Task<BulkOperationResult> HandleCommand(BulkCancelJourneysCommand request, CancellationToken cancellationToken)
    {
        var result = new BulkOperationResult
        {
            TotalCount = request.JourneyIds.Count,
            SuccessCount = 0,
            FailedCount = 0,
            FailedItems = new List<BulkOperationFailedItem>()
        };

        // Tüm journey'leri tek sorguda çek (performans için)
        var journeys = await context.Journeys
            .Include(x => x.Route)
            .Include(x => x.Stops)
            .Where(x => request.JourneyIds.Contains(x.Id) && x.Route.WorkspaceId == User.WorkspaceId)
            .ToListAsync(cancellationToken);

        foreach (var journeyId in request.JourneyIds)
        {
            var journey = journeys.FirstOrDefault(j => j.Id == journeyId);
            
            if (journey == null)
            {
                result.FailedCount++;
                result.FailedItems.Add(new BulkOperationFailedItem
                {
                    Id = journeyId,
                    Reason = "Sefer bulunamadı"
                });
                continue;
            }

            // Zaten iptal edilmiş veya tamamlanmış mı kontrol et
            if (journey.CancelledAt.HasValue)
            {
                result.FailedCount++;
                result.FailedItems.Add(new BulkOperationFailedItem
                {
                    Id = journeyId,
                    Name = journey.Route?.Name,
                    Reason = "Sefer zaten iptal edilmiş"
                });
                continue;
            }

            if (journey.CompletedAt.HasValue || journey.FinishedAt.HasValue)
            {
                result.FailedCount++;
                result.FailedItems.Add(new BulkOperationFailedItem
                {
                    Id = journeyId,
                    Name = journey.Route?.Name,
                    Reason = "Tamamlanmış seferler iptal edilemez"
                });
                continue;
            }

            // Journey'i iptal et
            journey.Cancel();

            // İlk stop için iptal status'ü ekle
            if (journey.Stops != null && journey.Stops.Any())
            {
                var firstStop = journey.Stops.OrderBy(s => s.Order).First();
                
                // JourneyStatus constructor'ı 3 parametre alıyor (journeyId, stopId, statusType)
                var cancelStatus = new JourneyStatus(
                    journey.Id,
                    firstStop.Id,
                    JourneyStatusType.Cancelled
                );
                
                // Notes ayrı olarak set edilebiliyorsa (eğer setter varsa)
                // Eğer Notes property'si read-only ise bu satırı silin
                // cancelStatus.Notes = request.Reason ?? "Toplu iptal işlemi";
                
                context.JourneyStatuses.Add(cancelStatus);
            }

            result.SuccessCount++;
        }

        await context.SaveChangesAsync(cancellationToken);

        result.Message = $"{result.SuccessCount} sefer başarıyla iptal edildi";
        if (result.FailedCount > 0)
        {
            result.Message += $", {result.FailedCount} sefer iptal edilemedi";
        }

        return result;
    }
}

// Bulk operation result DTO
public class BulkOperationResult
{
    public int TotalCount { get; set; }
    public int SuccessCount { get; set; }
    public int FailedCount { get; set; }
    public string Message { get; set; } = "";
    public List<BulkOperationFailedItem> FailedItems { get; set; } = new();
}

public class BulkOperationFailedItem
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string Reason { get; set; } = "";
}