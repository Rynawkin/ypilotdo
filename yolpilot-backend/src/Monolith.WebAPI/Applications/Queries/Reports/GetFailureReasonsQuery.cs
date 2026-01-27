using MediatR;
using Microsoft.EntityFrameworkCore;
using Monolith.WebAPI.Applications.Commands;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Data.Journeys;
using Monolith.WebAPI.Responses.Reports;
using Monolith.WebAPI.Services.Members;

namespace Monolith.WebAPI.Applications.Queries.Reports;

public class GetFailureReasonsQuery : BaseAuthenticatedCommand<IEnumerable<FailureReasonResponse>>
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string Period { get; set; } = "month";
    
    public override bool RequiresDriver => true;
    public override bool RequiresDispatcher => false;
    public override bool RequiresAdmin => false;
    public override bool RequiresSuperAdmin => false;
    public override bool RequiresWorkspaceAccess => false;
}

public class GetFailureReasonsQueryHandler : BaseAuthenticatedCommandHandler<GetFailureReasonsQuery, IEnumerable<FailureReasonResponse>>
{
    private readonly AppDbContext _context;

    public GetFailureReasonsQueryHandler(IUserService userService, AppDbContext context) 
        : base(userService)
    {
        _context = context;
    }

    protected override async Task<IEnumerable<FailureReasonResponse>> HandleCommand(GetFailureReasonsQuery request, CancellationToken cancellationToken)
    {
        // Tarih aralığını belirle
        var (fromDate, toDate) = GetDateRange(request.FromDate, request.ToDate, request.Period);

        // Journey statuses sorgusu - sadece failed olanlar
        var query = _context.JourneyStatuses
            .Include(js => js.Journey)
            .ThenInclude(j => j.Route)
            .Include(js => js.Journey)
            .ThenInclude(j => j.Driver)
            .Where(js => js.Journey.Route.WorkspaceId == User.WorkspaceId &&
                        js.Status == JourneyStatusType.Cancelled && // Failed stops için Cancelled kullanılıyor
                        js.CreatedAt >= fromDate &&
                        js.CreatedAt <= toDate &&
                        !string.IsNullOrEmpty(js.FailureReason));

        // Driver ise sadece kendi verilerini görebilir
        if (User.IsDriver && !User.IsDispatcher && !User.IsAdmin && !User.IsSuperAdmin)
        {
            query = query.Where(js => js.Journey.Driver != null && js.Journey.Driver.UserId == User.Id);
        }

        var failureStatuses = await query.ToListAsync(cancellationToken);

        // Failure reason'ları grup by yap ve say
        var failureReasons = failureStatuses
            .GroupBy(js => js.FailureReason.Trim())
            .Select(g => new FailureReasonResponse
            {
                Reason = g.Key,
                Count = g.Count(),
                Percentage = 0 // Aşağıda hesaplanacak
            })
            .OrderByDescending(fr => fr.Count)
            .ToList();

        // Toplam failure count'u hesapla ve yüzdeleri belirle
        var totalFailures = failureReasons.Sum(fr => fr.Count);
        if (totalFailures > 0)
        {
            foreach (var reason in failureReasons)
            {
                reason.Percentage = Math.Round((double)reason.Count / totalFailures * 100, 2);
            }
        }

        return failureReasons;
    }

    private (DateTime fromDate, DateTime toDate) GetDateRange(DateTime? fromDate, DateTime? toDate, string period)
    {
        var today = DateTime.Today;
        
        // Eğer manuel tarih verilmişse onu kullan
        if (fromDate.HasValue && toDate.HasValue)
        {
            return (fromDate.Value, toDate.Value.AddDays(1).AddSeconds(-1));
        }

        // Period'a göre tarih aralığı belirle
        return period.ToLower() switch
        {
            "today" => (today, today.AddDays(1).AddSeconds(-1)),
            "week" => (today.AddDays(-(int)today.DayOfWeek + 1), today.AddDays(-(int)today.DayOfWeek + 1).AddDays(7).AddSeconds(-1)),
            "quarter" => (today.AddMonths(-3), today.AddDays(1).AddSeconds(-1)),
            _ => (new DateTime(today.Year, today.Month, 1), new DateTime(today.Year, today.Month, 1).AddMonths(1).AddSeconds(-1))
        };
    }
}