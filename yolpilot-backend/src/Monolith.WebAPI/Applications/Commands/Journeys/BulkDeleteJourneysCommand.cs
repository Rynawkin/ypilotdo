using System.Text.Json.Serialization;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Data.Journeys;
using Monolith.WebAPI.Infrastructure;
using Monolith.WebAPI.Services.Members;

namespace Monolith.WebAPI.Applications.Commands.Journeys;

public class BulkDeleteJourneysCommand : BaseAuthenticatedCommand<BulkOperationResult>
{
    [JsonIgnore] public override bool RequiresAdmin => true; // Admin ve üstü yapabilir
    [JsonIgnore] public override bool RequiresWorkspaceAccess => true;
    public List<int> JourneyIds { get; set; } = new();
    public bool ForceDelete { get; set; } = false; // Aktif seferleri de silmek için
}

public class BulkDeleteJourneysCommandValidator : AbstractValidator<BulkDeleteJourneysCommand>
{
    public BulkDeleteJourneysCommandValidator()
    {
        RuleFor(x => x.JourneyIds)
            .NotEmpty().WithMessage("En az bir sefer seçmelisiniz")
            .Must(x => x.Count <= 50).WithMessage("Tek seferde maksimum 50 sefer silinebilir");
        
        RuleForEach(x => x.JourneyIds)
            .GreaterThan(0).WithMessage("Geçersiz sefer ID");
    }
}

public class BulkDeleteJourneysCommandHandler(AppDbContext context, IUserService userService)
    : BaseAuthenticatedCommandHandler<BulkDeleteJourneysCommand, BulkOperationResult>(userService)
{
    protected override async Task<BulkOperationResult> HandleCommand(BulkDeleteJourneysCommand request, CancellationToken cancellationToken)
    {
        var result = new BulkOperationResult
        {
            TotalCount = request.JourneyIds.Count,
            SuccessCount = 0,
            FailedCount = 0,
            FailedItems = new List<BulkOperationFailedItem>()
        };

        // Tüm journey'leri ve ilişkili verileri çek
        var journeys = await context.Journeys
            .Include(x => x.Route)
            .Include(x => x.Stops)
            .Include(x => x.Statuses)
            .Include(x => x.StartDetails)
            .Include(x => x.EndDetails)
            .Where(x => request.JourneyIds.Contains(x.Id) && x.Route.WorkspaceId == User.WorkspaceId)
            .ToListAsync(cancellationToken);

        var journeysToDelete = new List<Journey>();

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

            // Aktif seferler korunsun (ForceDelete false ise)
            // Journey'de status property'si olmadığı için StartedAt ve FinishedAt/CancelledAt ile kontrol
            bool isActive = journey.StartedAt.HasValue && 
                           !journey.FinishedAt.HasValue && 
                           !journey.CancelledAt.HasValue &&
                           !journey.CompletedAt.HasValue;
            
            if (!request.ForceDelete && isActive)
            {
                result.FailedCount++;
                result.FailedItems.Add(new BulkOperationFailedItem
                {
                    Id = journeyId,
                    Name = journey.Route?.Name,
                    Reason = "Aktif seferler silinemez. Önce iptal edin veya tamamlayın."
                });
                continue;
            }

            journeysToDelete.Add(journey);
            result.SuccessCount++;
        }

        if (journeysToDelete.Any())
        {
            // İlişkili verileri de sil (Cascade delete yoksa manuel olarak)
            foreach (var journey in journeysToDelete)
            {
                // JourneyStatus kayıtlarını sil
                if (journey.Statuses != null && journey.Statuses.Any())
                {
                    context.JourneyStatuses.RemoveRange(journey.Statuses);
                }

                // JourneyStop kayıtlarını sil
                if (journey.Stops != null && journey.Stops.Any())
                {
                    context.JourneyStops.RemoveRange(journey.Stops);
                }

                // StartDetails ve EndDetails varsa sil
                if (journey.StartDetails != null)
                {
                    context.JourneyStartDetails.Remove(journey.StartDetails);
                }
                
                if (journey.EndDetails != null)
                {
                    context.JourneyEndDetails.Remove(journey.EndDetails);
                }
            }

            // Journey'leri sil
            context.Journeys.RemoveRange(journeysToDelete);
            
            await context.SaveChangesAsync(cancellationToken);
        }

        result.Message = $"{result.SuccessCount} sefer kalıcı olarak silindi";
        if (result.FailedCount > 0)
        {
            result.Message += $", {result.FailedCount} sefer silinemedi";
        }

        return result;
    }
}