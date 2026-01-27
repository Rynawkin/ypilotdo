using MediatR;
using Microsoft.EntityFrameworkCore;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Infrastructure;
using Monolith.WebAPI.Responses.Journeys;
using Monolith.WebAPI.Responses.Workspace;

namespace Monolith.WebAPI.Applications.Queries.Journeys;

public record GetRouteQuery(int WorkspaceId, int RouteId) : IRequest<RouteResponse>;

public class GetRouteQueryHandler(AppDbContext context) : IRequestHandler<GetRouteQuery, RouteResponse>
{
    public async Task<RouteResponse> Handle(GetRouteQuery request, CancellationToken cancellationToken)
    {
        var route = await context.Routes
            .Include(x => x.Depot)
            .Include(x => x.Driver)
            .Include(x => x.Vehicle)
            .Include(x => x.Stops)
                .ThenInclude(s => s.Customer) // ✅ EKLENDI - Customer'ı include et
            .Include(x => x.StartDetails)
            .Include(x => x.EndDetails)
            .Where(x => x.WorkspaceId == request.WorkspaceId && x.Id == request.RouteId && !x.IsDeleted)
            .AsNoTracking()
            .AsSplitQuery()
            .FirstOrDefaultAsync(cancellationToken);

        if (route == null)
            throw new ApiException("Route not found.", 404);

        // ✅ DÜZELTİLDİ - Customer bilgileri artık entity'den geliyor, manuel matching yok
        return new RouteResponse(route);
    }
}