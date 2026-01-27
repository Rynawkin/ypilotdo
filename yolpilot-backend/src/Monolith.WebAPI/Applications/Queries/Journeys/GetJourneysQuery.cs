// src/Monolith.WebAPI/Applications/Queries/Journeys/GetJourneysQuery.cs
using MediatR;
using Microsoft.EntityFrameworkCore;
using Monolith.WebAPI.Applications.Commands;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Data.Journeys;
using Monolith.WebAPI.Data.Members;
using Monolith.WebAPI.Responses.Journeys;
using Monolith.WebAPI.Services.Members;

namespace Monolith.WebAPI.Applications.Queries.Journeys;

public class GetJourneysQuery : BaseAuthenticatedCommand<IEnumerable<JourneyResponse>>
{
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public string? Status { get; set; }
    public int? DriverId { get; set; }
    public int? VehicleId { get; set; }
    
    public override bool RequiresDriver => false;
}

public class GetJourneysQueryHandler : BaseAuthenticatedCommandHandler<GetJourneysQuery, IEnumerable<JourneyResponse>>
{
    private readonly AppDbContext _context;

    public GetJourneysQueryHandler(AppDbContext context, IUserService userService)
        : base(userService)
    {
        _context = context;
    }

    protected override async Task<IEnumerable<JourneyResponse>> HandleCommand(GetJourneysQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Journeys
            .AsNoTracking()
            .AsSplitQuery() // ⭐ CRITICAL FIX: Prevents cartesian explosion
            .Include(journey => journey.Driver)
            .Include(journey => journey.Vehicle)
            .Include(journey => journey.StartDetails)
            .Include(journey => journey.EndDetails)
            .Include(journey => journey.Route.Vehicle)
            .Include(journey => journey.Route.Driver)
            .Include(journey => journey.Stops.Where(s => s.Order <= 50).OrderBy(s => s.Order))
                .ThenInclude(stop => stop.RouteStop)
            .Include(journey => journey.Statuses.OrderByDescending(s => s.CreatedAt).Take(10))
            .Where(x => x.WorkspaceId == User.WorkspaceId);

        // Driver ise sadece kendisine atanan journey'leri görebilir
        if (User.IsDriver && !User.IsDispatcher && !User.IsAdmin && !User.IsSuperAdmin)
        {
            query = query.Where(j => j.Driver != null && j.Driver.UserId == User.Id);
        }

        // Tarih filtrelemesi
        if (request.From.HasValue)
        {
            query = query.Where(x => x.Date >= request.From.Value);
        }
        
        if (request.To.HasValue)
        {
            query = query.Where(x => x.Date <= request.To.Value);
        }
        
        if (!request.From.HasValue && !request.To.HasValue)
        {
            var thirtyDaysAgo = DateTime.Now.AddDays(-30);
            query = query.Where(x => x.Date >= thirtyDaysAgo);
        }

        // Status filtrelemesi - Journey.Status bir enum
if (!string.IsNullOrEmpty(request.Status))
{
    // String'i enum'a parse et
    if (Enum.TryParse<Data.Journeys.JourneyStatusEnum>(request.Status, true, out var statusEnum))
    {
        query = query.Where(x => x.Status == statusEnum);
    }
}

        // DriverId filtrelemesi
        if (request.DriverId.HasValue && User.CanAccessDispatcherFeatures())
        {
            query = query.Where(x => x.Driver != null && x.Driver.Id == request.DriverId.Value);
        }

        // VehicleId filtrelemesi
        if (request.VehicleId.HasValue && User.CanAccessDispatcherFeatures())
        {
            query = query.Where(x => x.VehicleId == request.VehicleId.Value);
        }

        var journeys = await query
            .OrderByDescending(x => x.Date)
            .ThenByDescending(x => x.StartedAt)
            .ToListAsync(cancellationToken);

        return journeys.Select(journey => new JourneyResponse(journey));
    }
}