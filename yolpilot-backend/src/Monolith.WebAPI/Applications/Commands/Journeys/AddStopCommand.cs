using System.Text.Json.Serialization;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Data.Journeys;
using Monolith.WebAPI.Infrastructure;
using Monolith.WebAPI.Requests;
using Monolith.WebAPI.Responses.Journeys;
using Monolith.WebAPI.Services.Members;
using Monolith.WebAPI.Services.Subscription;

namespace Monolith.WebAPI.Applications.Commands.Journeys;

public class AddRouteStopsCommand : BaseAuthenticatedCommand<List<RouteStopResponse>>
{
    [JsonIgnore] public override bool RequiresDispatcher => true; // Dispatcher ve üstü yapabilir
    [JsonIgnore] public override bool RequiresWorkspaceAccess => true;

    [JsonIgnore] public int RouteId { get; init; }

    public List<RouteStopRequest> Stops { get; init; }

    public List<RouteStop> ToEntities(TimeSpan defaultServiceTime) =>
        Stops.Select(stop => new RouteStop(stop, RouteId, defaultServiceTime)).ToList();
}

public class AddStopsCommandValidator : AbstractValidator<AddRouteStopsCommand>
{
    public AddStopsCommandValidator()
    {
        RuleFor(x => x.Stops).NotEmpty();
        RuleFor(x => x.RouteId).NotEmpty();
        RuleFor(x => x.AuthenticatedUserId).NotEmpty();

        RuleForEach(x => x.Stops).SetValidator(new StopRequestModelValidator());
    }
}

public class AddStopCommandHandler(AppDbContext context, IUserService userService, ISubscriptionService subscriptionService)
    : BaseAuthenticatedCommandHandler<AddRouteStopsCommand, List<RouteStopResponse>>(userService)
{
    override protected async Task<List<RouteStopResponse>> HandleCommand(AddRouteStopsCommand request,
        CancellationToken cancellationToken)
    {
        var route = await context.Routes
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.RouteId && x.WorkspaceId == User.WorkspaceId, cancellationToken);
        if (route is null)
            throw new ApiException("Route not found", 404);

        // Trial kullanıcıları için limit kontrolü - Yeni duraklar eklemeden önce kontrol et
        var stopCount = request.Stops.Count;
        for (int i = 0; i < stopCount; i++)
        {
            var canAddStop = await subscriptionService.CanAddStop(User.WorkspaceId);
            if (!canAddStop)
            {
                throw new ApiException("Trial limitinize ulaştınız. Devam etmek için plan yükseltin.", 403);
            }
            
            // Her durak için kullanım kaydet
            await subscriptionService.RecordStopUsage(User.WorkspaceId, 1);
        }

        var stops = request.ToEntities(User.Workspace.DefaultServiceTime);
        await context.RouteStops.AddRangeAsync(stops, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        return stops.Select(stop => new RouteStopResponse(stop)).ToList();
    }
}