using MediatR;
using Microsoft.EntityFrameworkCore;
using Monolith.WebAPI.Applications.Commands;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Data.Journeys;
using Monolith.WebAPI.Responses.Reports;
using Monolith.WebAPI.Services.Members;

namespace Monolith.WebAPI.Applications.Queries.Reports;

public class GetDriverPerformanceQuery : BaseAuthenticatedCommand<IEnumerable<DriverPerformanceResponse>>
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    
    public override bool RequiresDriver => true;
    public override bool RequiresDispatcher => false;
    public override bool RequiresAdmin => false;
    public override bool RequiresSuperAdmin => false;
    public override bool RequiresWorkspaceAccess => false;
}

public class GetDriverPerformanceQueryHandler : BaseAuthenticatedCommandHandler<GetDriverPerformanceQuery, IEnumerable<DriverPerformanceResponse>>
{
    private readonly AppDbContext _context;

    public GetDriverPerformanceQueryHandler(IUserService userService, AppDbContext context) 
        : base(userService)
    {
        _context = context;
    }

    protected override async Task<IEnumerable<DriverPerformanceResponse>> HandleCommand(GetDriverPerformanceQuery request, CancellationToken cancellationToken)
    {
        var fromDate = request.FromDate ?? DateTime.Today.AddDays(-30);
        var toDate = request.ToDate ?? DateTime.Today.AddDays(1).AddSeconds(-1);

        // Driver listesini al
        var driversQuery = _context.Drivers
            .Where(d => d.WorkspaceId == User.WorkspaceId);

        // Driver ise sadece kendisini görebilir
        if (User.IsDriver && !User.IsDispatcher && !User.IsAdmin && !User.IsSuperAdmin)
        {
            driversQuery = driversQuery.Where(d => d.UserId == User.Id);
        }

        var drivers = await driversQuery.ToListAsync(cancellationToken);

        // PERFORMANCE: Fetch all journeys in a single query to avoid N+1 problem
        var driverIds = drivers.Select(d => d.Id).ToList();
        var allJourneys = await _context.Journeys
            .Include(j => j.Stops)
            .Include(j => j.Route)
            .Where(j => driverIds.Contains(j.DriverId) &&
                       j.CreatedAt >= fromDate &&
                       j.CreatedAt <= toDate)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        // Group journeys by driver
        var journeysByDriver = allJourneys.GroupBy(j => j.DriverId)
                                          .ToDictionary(g => g.Key, g => g.ToList());

        var performance = new List<DriverPerformanceResponse>();

        foreach (var driver in drivers)
        {
            var journeys = journeysByDriver.GetValueOrDefault(driver.Id, new List<Journey>());

            var totalStops = journeys.SelectMany(j => j.Stops).Count();
            var completedStops = journeys.SelectMany(j => j.Stops).Count(s => s.Status == JourneyStopStatus.Completed);
            var failedStops = journeys.SelectMany(j => j.Stops).Count(s => s.Status == JourneyStopStatus.Failed);

            var avgTime = 0.0;
            if (journeys.Any(j => j.CompletedAt.HasValue && j.StartedAt.HasValue))
            {
                var completedJourneys = journeys.Where(j => j.CompletedAt.HasValue && j.StartedAt.HasValue);
                avgTime = completedJourneys.Average(j => (j.CompletedAt!.Value - j.StartedAt!.Value).TotalMinutes);
            }

            var totalDistance = journeys.Sum(j => j.Route?.TotalDistance ?? 0);

            performance.Add(new DriverPerformanceResponse
            {
                DriverId = driver.Id,
                DriverName = driver.Name,
                TotalDeliveries = totalStops,
                CompletedDeliveries = completedStops,
                FailedDeliveries = failedStops,
                AvgDeliveryTime = Math.Round(avgTime, 2),
                TotalDistance = Math.Round(totalDistance, 2),
                Rating = driver.Rating ?? 0
            });
        }

        return performance.OrderByDescending(p => p.CompletedDeliveries);
    }
}