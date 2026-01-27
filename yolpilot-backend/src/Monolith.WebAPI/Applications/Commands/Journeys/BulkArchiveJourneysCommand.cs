using System.Text.Json.Serialization;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Data.Journeys;
using Monolith.WebAPI.Infrastructure;
using Monolith.WebAPI.Services.Members;

namespace Monolith.WebAPI.Applications.Commands.Journeys;

public class BulkArchiveJourneysCommand : BaseAuthenticatedCommand<BulkOperationResult>
{
    [JsonIgnore] public override bool RequiresAdmin => true; // Admin ve üstü yapabilir
    [JsonIgnore] public override bool RequiresWorkspaceAccess => true;
    public List<int> JourneyIds { get; set; } = new();
}

public class BulkArchiveJourneysCommandValidator : AbstractValidator<BulkArchiveJourneysCommand>
{
    public BulkArchiveJourneysCommandValidator()
    {
        RuleFor(x => x.JourneyIds)
            .NotEmpty().WithMessage("En az bir sefer seçmelisiniz")
            .Must(x => x.Count <= 100).WithMessage("Tek seferde maksimum 100 sefer arşivlenebilir");
        
        RuleForEach(x => x.JourneyIds)
            .GreaterThan(0).WithMessage("Geçersiz sefer ID");
    }
}

public class BulkArchiveJourneysCommandHandler(AppDbContext context, IUserService userService)
    : BaseAuthenticatedCommandHandler<BulkArchiveJourneysCommand, BulkOperationResult>(userService)
{
    protected override async Task<BulkOperationResult> HandleCommand(BulkArchiveJourneysCommand request, CancellationToken cancellationToken)
    {
        var result = new BulkOperationResult
        {
            TotalCount = request.JourneyIds.Count,
            SuccessCount = 0,
            FailedCount = 0,
            FailedItems = new List<BulkOperationFailedItem>()
        };

        // Tüm journey'leri çek
        var journeys = await context.Journeys
            .Include(x => x.Route)
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

            // Zaten arşivlenmiş mi?
            if (journey.ArchivedAt.HasValue)
            {
                result.FailedCount++;
                result.FailedItems.Add(new BulkOperationFailedItem
                {
                    Id = journeyId,
                    Name = journey.Route?.Name,
                    Reason = "Sefer zaten arşivlenmiş"
                });
                continue;
            }

            // Aktif seferler arşivlenemez
            bool isActive = journey.StartedAt.HasValue && 
                           !journey.FinishedAt.HasValue && 
                           !journey.CancelledAt.HasValue &&
                           !journey.CompletedAt.HasValue;
            
            if (isActive)
            {
                result.FailedCount++;
                result.FailedItems.Add(new BulkOperationFailedItem
                {
                    Id = journeyId,
                    Name = journey.Route?.Name,
                    Reason = "Aktif seferler arşivlenemez"
                });
                continue;
            }

            // Arşivle
            journey.ArchivedAt = DateTime.UtcNow;
            journey.UpdatedAt = DateTime.UtcNow;

            result.SuccessCount++;
        }

        await context.SaveChangesAsync(cancellationToken);

        result.Message = $"{result.SuccessCount} sefer başarıyla arşivlendi";
        if (result.FailedCount > 0)
        {
            result.Message += $", {result.FailedCount} sefer arşivlenemedi";
        }

        return result;
    }
}