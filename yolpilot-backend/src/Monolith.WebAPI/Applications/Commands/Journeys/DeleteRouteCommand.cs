using System.Text.Json.Serialization;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Infrastructure;
using Monolith.WebAPI.Services.Members;

namespace Monolith.WebAPI.Applications.Commands.Journeys;

public class DeleteRouteCommand : BaseAuthenticatedCommand<bool>
{
    [JsonIgnore] public override bool RequiresDispatcher => true; // Dispatcher ve üstü yapabilir
    [JsonIgnore] public override bool RequiresWorkspaceAccess => true;
    public int RouteId { get; set; }
}

public class DeleteRouteCommandValidator : AbstractValidator<DeleteRouteCommand>
{
    public DeleteRouteCommandValidator()
    {
        RuleFor(x => x.RouteId).GreaterThan(0);
    }
}

public class DeleteRouteCommandHandler(AppDbContext context, IUserService userService)
    : BaseAuthenticatedCommandHandler<DeleteRouteCommand, bool>(userService)
{
    protected override async Task<bool> HandleCommand(DeleteRouteCommand request, CancellationToken cancellationToken)
    {
        var route = await context.Routes
            .Include(x => x.Stops)
            .FirstOrDefaultAsync(x => x.Id == request.RouteId && x.WorkspaceId == User.WorkspaceId, cancellationToken);

        if (route == null)
            throw new ApiException("Route not found.", 404);

        // Check if route has any journeys
        var hasJourneys = await context.Journeys
            .AnyAsync(j => j.RouteId == request.RouteId, cancellationToken);

        if (hasJourneys)
        {
            // Check if any journey is active
            var hasActiveJourney = await context.Journeys
                .AnyAsync(j => j.RouteId == request.RouteId && 
                              !j.FinishedAt.HasValue && 
                              !j.CompletedAt.HasValue, cancellationToken);

            if (hasActiveJourney)
                throw new ApiException("Cannot delete route with active journey.", 400);
        }

        // Remove stops first
        if (route.Stops != null && route.Stops.Any())
        {
            context.RemoveRange(route.Stops);
        }
        
        // Remove route
        context.Routes.Remove(route);
        
        await context.SaveChangesAsync(cancellationToken);
        
        return true;
    }
}