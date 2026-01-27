using MediatR;
using Microsoft.EntityFrameworkCore;
using Monolith.WebAPI.Applications.Commands;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Infrastructure;
using Monolith.WebAPI.Responses.Journeys;
using Monolith.WebAPI.Services.Members;

namespace Monolith.WebAPI.Applications.Queries.Journeys;

public class GetJourneyQuery : BaseAuthenticatedCommand<JourneyResponse>
{
    public int JourneyId { get; set; }
    
    // Herkes erişebilir ama driver sadece kendisininkileri görebilir
    public override bool RequiresDriver => false;
}

public class GetJourneyQueryHandler : BaseAuthenticatedCommandHandler<GetJourneyQuery, JourneyResponse>
{
    private readonly AppDbContext _context;

    public GetJourneyQueryHandler(AppDbContext context, IUserService userService)
        : base(userService)
    {
        _context = context;
    }

    protected override async Task<JourneyResponse> HandleCommand(GetJourneyQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Journeys
            .AsNoTracking()
            .AsSplitQuery() // ⭐ CRITICAL FIX: Prevents cartesian explosion
            .Include(journey => journey.Stops)
                .ThenInclude(stop => stop.RouteStop)
                    .ThenInclude(routeStop => routeStop.Customer)
            .Include(journey => journey.StartDetails)
            .Include(journey => journey.EndDetails)
            .Include(journey => journey.Statuses.OrderByDescending(s => s.CreatedAt).Take(50)) // Limit statuses
            .Include(journey => journey.Driver)
            .Include(journey => journey.Vehicle) // V38 - Vehicle ilişkisi
            .Include(journey => journey.Route)
                .ThenInclude(route => route.Stops.Where(s => !s.IsExcluded))
                    .ThenInclude(stop => stop.Customer)
            .Include(journey => journey.Route.Driver)
            .Include(journey => journey.Route.Vehicle)
            .Include(journey => journey.Route.Depot)
            .Include(journey => journey.Route.StartDetails)
            .Include(journey => journey.Route.EndDetails)
            .Where(x => x.Id == request.JourneyId && x.Route.WorkspaceId == User.WorkspaceId);

        // ✅ ROL BAZLI FİLTRELEME
        // Driver ise sadece kendisine atanan journey'yi görebilir
        if (User.IsDriver && !User.IsDispatcher && !User.IsAdmin && !User.IsSuperAdmin)
        {
            // ✅ DÜZELTME: Driver.UserId artık mevcut
            query = query.Where(j => j.Driver != null && j.Driver.UserId == User.Id);
        }

        var journey = await query.FirstOrDefaultAsync(cancellationToken);

        if (journey is null)
            throw new ApiException("Journey not found or access denied", 404);

        return new JourneyResponse(journey);
    }
}