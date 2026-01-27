using MediatR;
using Microsoft.EntityFrameworkCore;
using Monolith.WebAPI.Applications.Commands;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Data.Journeys;
using Monolith.WebAPI.Responses.Reports;
using Monolith.WebAPI.Services.Members;

namespace Monolith.WebAPI.Applications.Queries.Reports;

public class GetDeliveryTrendsQuery : BaseAuthenticatedCommand<IEnumerable<DeliveryTrendResponse>>
{
    public int Days { get; set; } = 7;
    
    public override bool RequiresDriver => true;
    public override bool RequiresDispatcher => false;
    public override bool RequiresAdmin => false;
    public override bool RequiresSuperAdmin => false;
    public override bool RequiresWorkspaceAccess => false;
}

public class GetDeliveryTrendsQueryHandler : BaseAuthenticatedCommandHandler<GetDeliveryTrendsQuery, IEnumerable<DeliveryTrendResponse>>
{
    private readonly AppDbContext _context;

    public GetDeliveryTrendsQueryHandler(IUserService userService, AppDbContext context) 
        : base(userService)
    {
        _context = context;
    }

    protected override async Task<IEnumerable<DeliveryTrendResponse>> HandleCommand(GetDeliveryTrendsQuery request, CancellationToken cancellationToken)
    {
        var endDate = DateTime.Today.AddDays(1).AddSeconds(-1);
        var startDate = DateTime.Today.AddDays(-request.Days + 1);
        
        var trends = new List<DeliveryTrendResponse>();

        for (var date = startDate; date <= endDate; date = date.AddDays(1))
        {
            var dayStart = date;
            var dayEnd = date.AddDays(1).AddSeconds(-1);

            var query = _context.Journeys
                .Include(j => j.Stops)
                .Include(j => j.Driver)
                .Where(j => j.Route.WorkspaceId == User.WorkspaceId &&
                           j.CreatedAt >= dayStart &&
                           j.CreatedAt <= dayEnd);

            // Driver ise sadece kendi verilerini görebilir
            if (User.IsDriver && !User.IsDispatcher && !User.IsAdmin && !User.IsSuperAdmin)
            {
                query = query.Where(j => j.Driver != null && j.Driver.UserId == User.Id);
            }

            var journeys = await query.ToListAsync(cancellationToken);

            var completed = journeys
                .SelectMany(j => j.Stops)
                .Count(s => s.Status == JourneyStopStatus.Completed);

            var failed = journeys
                .SelectMany(j => j.Stops)
                .Count(s => s.Status == JourneyStopStatus.Failed);

            var total = journeys
                .SelectMany(j => j.Stops)
                .Count();

            trends.Add(new DeliveryTrendResponse
            {
                Date = date.ToString("yyyy-MM-dd"),
                Completed = completed,
                Failed = failed,
                Total = total
            });
        }

        return trends;
    }
}