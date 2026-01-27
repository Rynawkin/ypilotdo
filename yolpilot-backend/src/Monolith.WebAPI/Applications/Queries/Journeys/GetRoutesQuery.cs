using MediatR;
using Microsoft.EntityFrameworkCore;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Responses.Journeys;
using Monolith.WebAPI.Responses.Workspace;

namespace Monolith.WebAPI.Applications.Queries.Journeys;

public record GetRoutesQuery(
    int WorkspaceId,
    int? Limit = 100,           // Default 100 route limit
    int? DaysBack = 90          // Default last 90 days
) : IRequest<IEnumerable<RouteResponse>>;

public class GetRoutesQueryHandler(AppDbContext context) : IRequestHandler<GetRoutesQuery, IEnumerable<RouteResponse>>
{
    public async Task<IEnumerable<RouteResponse>> Handle(GetRoutesQuery request, CancellationToken cancellationToken)
    {
        // Date filter - only recent routes
        var cutoffDate = DateTime.UtcNow.AddDays(-(request.DaysBack ?? 90));

        var routes = await context.Routes
            .Include(x => x.Depot)
            .Include(x => x.Driver)
            .Include(x => x.Vehicle)
            .Include(x => x.Stops)
            .Include(x => x.StartDetails)
            .Include(x => x.EndDetails)
            .Where(x => x.WorkspaceId == request.WorkspaceId
                && !x.IsDeleted
                && x.Date >= cutoffDate)  // PERFORMANCE: Only recent routes
            .AsNoTracking()
            .AsSplitQuery()
            .OrderByDescending(x => x.Date)
            .ThenByDescending(x => x.Id)
            .Take(request.Limit ?? 100)  // PERFORMANCE: Limit results
            .ToListAsync(cancellationToken);

        var responses = new List<RouteResponse>();
        
        // Customer bilgilerini bir kere yükle
        var customers = await context.Customers
            .Where(c => c.WorkspaceId == request.WorkspaceId && !c.IsDeleted)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        foreach (var route in routes)
        {
            var response = new RouteResponse(route);
            
            // Her route'un stop'larına customer bilgilerini ekle
            if (response.Stops != null && response.Stops.Any())
            {
                foreach (var stop in response.Stops)
                {
                    var customer = customers.FirstOrDefault(c => 
                        c.Name == stop.Name && c.Address == stop.Address);
                    
                    if (customer != null)
                    {
                        stop.CustomerId = customer.Id;
                        stop.Customer = new CustomerResponse
                        {
                            Id = customer.Id,
                            Code = customer.Code,
                            Name = customer.Name,
                            Address = customer.Address,
                            Phone = customer.Phone ?? "",
                            Email = customer.Email,
                            Latitude = customer.Latitude,
                            Longitude = customer.Longitude,
                            CreatedAt = customer.CreatedAt,
                            UpdatedAt = customer.UpdatedAt ?? customer.CreatedAt
                        };
                    }
                }
            }
            
            responses.Add(response);
        }

        return responses;
    }
}