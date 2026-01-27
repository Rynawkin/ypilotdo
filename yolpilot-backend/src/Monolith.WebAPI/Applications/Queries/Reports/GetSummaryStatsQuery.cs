using MediatR;
using Microsoft.EntityFrameworkCore;
using Monolith.WebAPI.Applications.Commands;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Data.Journeys;
using Monolith.WebAPI.Responses.Reports;
using Monolith.WebAPI.Services.Members;

namespace Monolith.WebAPI.Applications.Queries.Reports;

public class GetSummaryStatsQuery : BaseAuthenticatedCommand<SummaryStatsResponse>
{
    public string Period { get; set; } = "month"; // today, week, month, quarter
    
    public override bool RequiresDriver => true;
    public override bool RequiresDispatcher => false;
    public override bool RequiresAdmin => false;
    public override bool RequiresSuperAdmin => false;
    public override bool RequiresWorkspaceAccess => false;
}

public class GetSummaryStatsQueryHandler : BaseAuthenticatedCommandHandler<GetSummaryStatsQuery, SummaryStatsResponse>
{
    private readonly AppDbContext _context;

    public GetSummaryStatsQueryHandler(IUserService userService, AppDbContext context) 
        : base(userService)
    {
        _context = context;
    }

    protected override async Task<SummaryStatsResponse> HandleCommand(GetSummaryStatsQuery request, CancellationToken cancellationToken)
    {
        // Tarih aralığını belirle
        var (currentStart, currentEnd, previousStart, previousEnd) = GetDateRanges(request.Period);

        // PERFORMANCE: Fetch both periods in a single query
        var allJourneysQuery = _context.Journeys
            .Include(j => j.Stops)
            .Include(j => j.Route)
            .Include(j => j.Driver)
            .Where(j => j.Route.WorkspaceId == User.WorkspaceId &&
                       j.CreatedAt >= previousStart && // Start from previous period start
                       j.CreatedAt <= currentEnd);     // End at current period end

        // Driver ise sadece kendi verilerini görebilir
        if (User.IsDriver && !User.IsDispatcher && !User.IsAdmin && !User.IsSuperAdmin)
        {
            allJourneysQuery = allJourneysQuery.Where(j => j.Driver != null && j.Driver.UserId == User.Id);
        }

        var allJourneys = await allJourneysQuery.AsNoTracking().ToListAsync(cancellationToken);

        // Split into current and previous periods
        var currentJourneys = allJourneys.Where(j => j.CreatedAt >= currentStart && j.CreatedAt <= currentEnd).ToList();
        var previousJourneys = allJourneys.Where(j => j.CreatedAt >= previousStart && j.CreatedAt <= previousEnd).ToList();

        // Mevcut dönem metrikleri
        var totalDeliveries = currentJourneys.SelectMany(j => j.Stops).Count();
        var completedDeliveries = currentJourneys.SelectMany(j => j.Stops).Count(s => s.Status == JourneyStopStatus.Completed);
        var failedDeliveries = currentJourneys.SelectMany(j => j.Stops).Count(s => s.Status == JourneyStopStatus.Failed);
        var successRate = totalDeliveries > 0 ? Math.Round((double)completedDeliveries / totalDeliveries * 100, 2) : 0;
        
        var avgTime = 0.0;
        var completedJourneysWithTime = currentJourneys.Where(j => j.CompletedAt.HasValue && j.StartedAt.HasValue).ToList();
        if (completedJourneysWithTime.Any())
        {
            avgTime = completedJourneysWithTime.Average(j => (j.CompletedAt!.Value - j.StartedAt!.Value).TotalMinutes);
        }

        var totalDistance = currentJourneys.Sum(j => j.Route?.TotalDistance ?? 0);

        // Önceki dönem metrikleri (değişim hesaplama için)
        var prevTotalDeliveries = previousJourneys.SelectMany(j => j.Stops).Count();
        var prevCompletedDeliveries = previousJourneys.SelectMany(j => j.Stops).Count(s => s.Status == JourneyStopStatus.Completed);
        var prevSuccessRate = prevTotalDeliveries > 0 ? (double)prevCompletedDeliveries / prevTotalDeliveries * 100 : 0;
        
        var prevAvgTime = 0.0;
        var prevCompletedJourneysWithTime = previousJourneys.Where(j => j.CompletedAt.HasValue && j.StartedAt.HasValue).ToList();
        if (prevCompletedJourneysWithTime.Any())
        {
            prevAvgTime = prevCompletedJourneysWithTime.Average(j => (j.CompletedAt!.Value - j.StartedAt!.Value).TotalMinutes);
        }

        // Değişim yüzdelerini hesapla
        var deliveryChange = prevTotalDeliveries > 0 ? ((double)(totalDeliveries - prevTotalDeliveries) / prevTotalDeliveries * 100) : 0;
        var successRateChange = successRate - prevSuccessRate;
        var avgTimeChange = prevAvgTime > 0 ? ((avgTime - prevAvgTime) / prevAvgTime * 100) : 0;

        // Aktif driver ve araç sayısı
        var activeDrivers = 0;
        var activeVehicles = 0;
        var totalCustomers = 0;

        if (User.IsDispatcher || User.IsAdmin || User.IsSuperAdmin)
        {
            activeDrivers = await _context.Drivers
                .Where(d => d.WorkspaceId == User.WorkspaceId && 
                           (d.Status == "available" || d.Status == "busy"))
                .CountAsync(cancellationToken);

            activeVehicles = await _context.Vehicles
                .Where(v => v.WorkspaceId == User.WorkspaceId && v.Status == "active")
                .CountAsync(cancellationToken);

            totalCustomers = await _context.Customers
                .Where(c => c.WorkspaceId == User.WorkspaceId)
                .CountAsync(cancellationToken);
        }
        else if (User.IsDriver)
        {
            activeDrivers = 1; // Kendisi
            activeVehicles = currentJourneys.Select(j => j.Route?.VehicleId).Distinct().Count(v => v.HasValue);
        }

        var activeRoutes = currentJourneys.Select(j => j.RouteId).Distinct().Count();

        return new SummaryStatsResponse
        {
            TotalDeliveries = totalDeliveries,
            CompletedDeliveries = completedDeliveries,
            FailedDeliveries = failedDeliveries,
            SuccessRate = successRate,
            AvgDeliveryTime = Math.Round(avgTime, 2),
            TotalDistance = Math.Round(totalDistance, 2),
            ActiveDrivers = activeDrivers,
            ActiveVehicles = activeVehicles,
            TotalCustomers = totalCustomers,
            ActiveRoutes = activeRoutes,
            DeliveryChange = Math.Round(deliveryChange, 2),
            SuccessRateChange = Math.Round(successRateChange, 2),
            AvgTimeChange = Math.Round(avgTimeChange, 2)
        };
    }

    private (DateTime currentStart, DateTime currentEnd, DateTime previousStart, DateTime previousEnd) GetDateRanges(string period)
    {
        var today = DateTime.Today;
        DateTime currentStart, currentEnd, previousStart, previousEnd;

        switch (period.ToLower())
        {
            case "today":
                currentStart = today;
                currentEnd = today.AddDays(1).AddSeconds(-1);
                previousStart = today.AddDays(-1);
                previousEnd = today.AddSeconds(-1);
                break;
            case "week":
                var weekStart = today.AddDays(-(int)today.DayOfWeek + 1); // Pazartesi
                currentStart = weekStart;
                currentEnd = weekStart.AddDays(7).AddSeconds(-1);
                previousStart = weekStart.AddDays(-7);
                previousEnd = weekStart.AddSeconds(-1);
                break;
            case "quarter":
                currentStart = today.AddMonths(-3);
                currentEnd = today.AddDays(1).AddSeconds(-1);
                previousStart = today.AddMonths(-6);
                previousEnd = today.AddMonths(-3).AddSeconds(-1);
                break;
            case "month":
            default:
                currentStart = new DateTime(today.Year, today.Month, 1);
                currentEnd = currentStart.AddMonths(1).AddSeconds(-1);
                previousStart = currentStart.AddMonths(-1);
                previousEnd = currentStart.AddSeconds(-1);
                break;
        }

        return (currentStart, currentEnd, previousStart, previousEnd);
    }
}