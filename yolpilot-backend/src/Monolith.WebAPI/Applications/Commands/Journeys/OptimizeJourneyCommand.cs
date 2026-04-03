using System.Text.Json.Serialization;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Data.Journeys;
using Monolith.WebAPI.Infrastructure;
using Monolith.WebAPI.Responses.Journeys;
using Monolith.WebAPI.Services.Members;

namespace Monolith.WebAPI.Applications.Commands.Journeys;

public class OptimizeJourneyCommand : BaseAuthenticatedCommand<JourneyResponse>
{
    [JsonIgnore] public override bool RequiresDispatcher => true; // Dispatcher ve üstü yapabilir
    [JsonIgnore] public override bool RequiresWorkspaceAccess => true;
    [JsonIgnore] public int JourneyId { get; set; }
}

public class OptimizeJourneyCommandValidator : AbstractValidator<OptimizeJourneyCommand>
{
    public OptimizeJourneyCommandValidator()
    {
        RuleFor(x => x.JourneyId).NotEmpty();
    }
}

public class OptimizeJourneyCommandHandler(
    AppDbContext context,
    IUserService userService,
    ISender sender)
    : BaseAuthenticatedCommandHandler<OptimizeJourneyCommand, JourneyResponse>(userService)
{
    override protected async Task<JourneyResponse> HandleCommand(OptimizeJourneyCommand request, CancellationToken cancellationToken)
    {
        var journey = await context.Journeys
            .Include(x => x.StartDetails)
            .Include(x => x.EndDetails)
            .Include(x => x.Route)
                .ThenInclude(x => x.Workspace)
            .Include(x => x.Route)
                .ThenInclude(x => x.Stops)
                    .ThenInclude(s => s.Customer)
            .Include(x => x.Driver)
            .Include(x => x.Stops)
            .AsSplitQuery()
            .FirstOrDefaultAsync(x => x.Id == request.JourneyId && x.Route.WorkspaceId == User.WorkspaceId, cancellationToken);

        if (journey is null)
            throw new ApiException("Journey not found", 404);

        if (journey.Route?.Stops is null || journey.Route?.Stops?.Count == 0)
            throw new ApiException("Route stops not found", 404);

        var optimizeRouteCommand = new OptimizeRouteCommand
        {
            RouteId = journey.RouteId,
            AuthenticatedUserId = request.AuthenticatedUserId
        };

        await sender.Send(optimizeRouteCommand, cancellationToken);

        journey = await context.Journeys
            .Include(x => x.StartDetails)
            .Include(x => x.EndDetails)
            .Include(x => x.Route)
                .ThenInclude(x => x.Workspace)
            .Include(x => x.Route)
                .ThenInclude(x => x.Stops)
                    .ThenInclude(s => s.Customer)
            .Include(x => x.Driver)
            .Include(x => x.Stops)
            .AsSplitQuery()
            .FirstOrDefaultAsync(x => x.Id == request.JourneyId && x.Route.WorkspaceId == User.WorkspaceId, cancellationToken);

        if (journey is null)
            throw new ApiException("Journey not found", 404);

        journey.CreateStopsFromRoutePlan();

        // Journey stops'larını ekle
        foreach (var s in journey.Stops)
            context.Entry(s).State = EntityState.Added;

        // Route'u güncelle
        if (journey.Route != null)
        {
            context.Routes.Update(journey.Route);
        }

        context.Journeys.Update(journey);
        await context.SaveChangesAsync(cancellationToken);

        // Journey'i tüm ilişkili verilerle tekrar yükle
        journey = await context.Journeys
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
            .FirstOrDefaultAsync(x => x.Id == request.JourneyId, cancellationToken);

        return new JourneyResponse(journey);
    }
}
