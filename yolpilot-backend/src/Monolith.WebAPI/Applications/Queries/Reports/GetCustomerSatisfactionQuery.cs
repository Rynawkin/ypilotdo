using MediatR;
using Microsoft.EntityFrameworkCore;
using Monolith.WebAPI.Applications.Commands;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Responses.Reports;
using Monolith.WebAPI.Services.Members;

namespace Monolith.WebAPI.Applications.Queries.Reports;

public class GetCustomerSatisfactionQuery : BaseAuthenticatedCommand<CustomerSatisfactionResponse>
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    
    public override bool RequiresDriver => false;
    public override bool RequiresDispatcher => true; // Sadece Dispatcher ve üzeri
    public override bool RequiresAdmin => false;
    public override bool RequiresSuperAdmin => false;
    public override bool RequiresWorkspaceAccess => false;
}

public class GetCustomerSatisfactionQueryHandler : BaseAuthenticatedCommandHandler<GetCustomerSatisfactionQuery, CustomerSatisfactionResponse>
{
    private readonly AppDbContext _context;

    public GetCustomerSatisfactionQueryHandler(IUserService userService, AppDbContext context) 
        : base(userService)
    {
        _context = context;
    }

    protected override async Task<CustomerSatisfactionResponse> HandleCommand(GetCustomerSatisfactionQuery request, CancellationToken cancellationToken)
    {
        var fromDate = request.FromDate ?? DateTime.Today.AddDays(-30);
        var toDate = request.ToDate ?? DateTime.Today.AddDays(1).AddSeconds(-1);

        // Müşterileri al
        var customers = await _context.Customers
            .Where(c => c.WorkspaceId == User.WorkspaceId)
            .ToListAsync(cancellationToken);

        // Öncelik dağılımı
        var priorityDistribution = customers
            .GroupBy(c => c.Priority)
            .Select(g => new CustomerPriorityData
            {
                Priority = g.Key == "high" ? "Yüksek Öncelik" : 
                          g.Key == "normal" ? "Normal Öncelik" : "Düşük Öncelik",
                Count = g.Count(),
                Percentage = Math.Round((double)g.Count() / customers.Count * 100, 2)
            })
            .ToList();

        // Top müşteriler (en çok teslimat yapılan)
        var topCustomers = new List<TopCustomerData>();
        
        foreach (var customer in customers.Take(10))
        {
            var deliveryCount = await _context.RouteStops
                .Include(rs => rs.Route)
                    .ThenInclude(r => r.Journeys)
                .Where(rs => rs.CustomerId == customer.Id &&
                            rs.Route.CreatedAt >= fromDate &&
                            rs.Route.CreatedAt <= toDate)
                .CountAsync(cancellationToken);

            topCustomers.Add(new TopCustomerData
            {
                CustomerId = customer.Id,
                CustomerName = customer.Name,
                Address = customer.Address,
                TotalDeliveries = deliveryCount,
                Priority = customer.Priority
            });
        }

        topCustomers = topCustomers
            .OrderByDescending(c => c.TotalDeliveries)
            .Take(5)
            .ToList();

        // Rating dağılımı (mock data - gerçek rating sistemi eklendiğinde güncellenecek)
        var ratingDistribution = new Dictionary<string, int>
        {
            { "5", 45 },
            { "4", 30 },
            { "3", 15 },
            { "2", 7 },
            { "1", 3 }
        };

        var totalRatings = ratingDistribution.Values.Sum();
        var overallRating = 0.0;
        
        if (totalRatings > 0)
        {
            overallRating = ratingDistribution.Sum(r => int.Parse(r.Key) * r.Value) / (double)totalRatings;
        }

        return new CustomerSatisfactionResponse
        {
            OverallRating = Math.Round(overallRating, 2),
            TotalRatings = totalRatings,
            RatingDistribution = ratingDistribution,
            PriorityDistribution = priorityDistribution,
            TopCustomers = topCustomers
        };
    }
}