using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Infrastructure;
using Monolith.WebAPI.Services.Members;

namespace Monolith.WebAPI.Applications.Commands.Journeys;

public class DeleteStopCommand : BaseAuthenticatedCommand<bool>
{
    [JsonIgnore] public override bool RequiresDispatcher => true; // Dispatcher ve üstü yapabilir
    [JsonIgnore] public override bool RequiresWorkspaceAccess => true;

    public int StopId { get; set; }
    public int RouteId { get; set; }
}

public class DeleteStopCommandHandler(AppDbContext context, IUserService userService)
    : BaseAuthenticatedCommandHandler<DeleteStopCommand, bool>(userService)
{
    override protected async Task<bool> HandleCommand(DeleteStopCommand request, CancellationToken cancellationToken)
    {
        var route = await context.Routes
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.RouteId && x.WorkspaceId == User.WorkspaceId, cancellationToken);
        if (route is null)
            throw new ApiException("Route not found", 404);

        var stop = await context.RouteStops
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.StopId && x.RouteId == route.Id, cancellationToken);
        if (stop is null)
            throw new ApiException("Stop not found", 404);

        context.RouteStops.Remove(stop);
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }
}