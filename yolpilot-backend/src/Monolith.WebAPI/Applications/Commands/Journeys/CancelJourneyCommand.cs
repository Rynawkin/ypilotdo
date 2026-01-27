using System.Text.Json.Serialization;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Data.Journeys;
using Monolith.WebAPI.Infrastructure;
using Monolith.WebAPI.Responses.Journeys;
using Monolith.WebAPI.Services.Members;

namespace Monolith.WebAPI.Applications.Commands.Journeys;

public class CancelJourneyCommand : BaseAuthenticatedCommand<JourneyResponse>
{
    [JsonIgnore] public override bool RequiresDispatcher => true; // Dispatcher ve üstü yapabilir
    [JsonIgnore] public override bool RequiresWorkspaceAccess => true;
    public int JourneyId { get; set; }
}

public class CancelJourneyCommandValidator : AbstractValidator<CancelJourneyCommand>
{
    public CancelJourneyCommandValidator()
    {
        RuleFor(x => x.JourneyId).GreaterThan(0);
    }
}

public class CancelJourneyCommandHandler(AppDbContext context, IUserService userService)
    : BaseAuthenticatedCommandHandler<CancelJourneyCommand, JourneyResponse>(userService)
{
    protected override async Task<JourneyResponse> HandleCommand(CancelJourneyCommand request, CancellationToken cancellationToken)
    {
        var journey = await context.Journeys
            .Include(x => x.Route)
                .ThenInclude(r => r.Stops)
                    .ThenInclude(s => s.Customer)
            .Include(x => x.Route)
                .ThenInclude(r => r.Driver)
            .Include(x => x.Route)
                .ThenInclude(r => r.Vehicle)
            .Include(x => x.Route)
                .ThenInclude(r => r.Depot)
            .Include(x => x.Route)
                .ThenInclude(r => r.StartDetails)
            .Include(x => x.Route)
                .ThenInclude(r => r.EndDetails)
            .Include(x => x.Stops)
                .ThenInclude(s => s.RouteStop)
                    .ThenInclude(rs => rs.Customer)
            .Include(x => x.Driver)
            .Include(x => x.StartDetails)
            .Include(x => x.EndDetails)
            .Include(x => x.Statuses)
            .FirstOrDefaultAsync(x => x.Id == request.JourneyId && x.Route.WorkspaceId == User.WorkspaceId, cancellationToken);

        if (journey == null)
            throw new ApiException("Journey not found.", 404);

        // Journey zaten iptal edilmiş veya tamamlanmış mı kontrol et
        if (journey.CancelledAt.HasValue)
            throw new ApiException("Journey is already cancelled.", 400);
            
        if (journey.CompletedAt.HasValue)
            throw new ApiException("Journey is already completed.", 400);

        if (journey.FinishedAt.HasValue)
            throw new ApiException("Journey is already finished.", 400);

        // Journey'i iptal et - Cancel() metodunu kullan
        journey.Cancel();
        
        // JourneyStatus tablosuna iptal kaydı ekle
        // Eğer journey'de stop varsa ilk stop'un ID'sini kullan
        if (journey.Stops != null && journey.Stops.Any())
        {
            var firstStop = journey.Stops.OrderBy(s => s.Order).First();
            var cancelStatus = new JourneyStatus(
                journey.Id, 
                firstStop.Id,
                JourneyStatusType.Cancelled
            );
            context.JourneyStatuses.Add(cancelStatus);
        }
        // Eğer stop yoksa sadece journey'i iptal et, status ekleme

        await context.SaveChangesAsync(cancellationToken);

        return new JourneyResponse(journey);
    }
}