// src/Monolith.WebAPI/Applications/Queries/Admin/GetAllWorkspacesQuery.cs
using MediatR;
using Microsoft.EntityFrameworkCore;
using Monolith.WebAPI.Applications.Commands;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Responses.Admin;
using Monolith.WebAPI.Services.Members;

namespace Monolith.WebAPI.Applications.Queries.Admin;

public class GetAllWorkspacesQuery : BaseAuthenticatedCommand<List<WorkspaceListResponse>>
{
    public string? SearchQuery { get; set; }
    public bool? ActiveOnly { get; set; }
    
    // ✅ SADECE SuperAdmin tüm workspace'leri görebilir
    public override bool RequiresSuperAdmin => true;
}

public class GetAllWorkspacesQueryHandler : BaseAuthenticatedCommandHandler<GetAllWorkspacesQuery, List<WorkspaceListResponse>>
{
    private readonly AppDbContext _context;

    public GetAllWorkspacesQueryHandler(AppDbContext context, IUserService userService) 
        : base(userService)
    {
        _context = context;
    }

    protected override async Task<List<WorkspaceListResponse>> HandleCommand(GetAllWorkspacesQuery request, CancellationToken cancellationToken)
    {
        // ✅ Sadece SuperAdmin bu noktaya gelebilir
        
        var query = _context.Workspaces.AsQueryable();

        // Active filtresi
        if (request.ActiveOnly.HasValue && request.ActiveOnly.Value)
        {
            query = query.Where(w => w.Active);
        }

        // Arama filtresi
        if (!string.IsNullOrEmpty(request.SearchQuery))
        {
            var searchLower = request.SearchQuery.ToLower();
            query = query.Where(w => 
                w.Name.ToLower().Contains(searchLower) ||
                w.Email.ToLower().Contains(searchLower) ||
                (w.PhoneNumber != null && w.PhoneNumber.Contains(searchLower)));
        }

        var workspaces = await query
            .OrderByDescending(w => w.CreatedAt)
            .ToListAsync(cancellationToken);

        return workspaces.Select(w => new WorkspaceListResponse
        {
            Id = w.Id,
            Name = w.Name,
            Email = w.Email,
            PhoneNumber = w.PhoneNumber,
            DistanceUnit = w.DistanceUnit,
            Currency = w.Currency,
            TimeZone = w.TimeZone,
            Language = w.Language,
            DefaultServiceTime = w.DefaultServiceTime,
            MaximumDriverCount = w.MaximumDriverCount,
            Active = w.Active,
            CreatedAt = w.CreatedAt,
            Subscription = new SubscriptionResponse
            {
                Plan = GetPlanType(w.CreatedAt),
                StartDate = w.CreatedAt,
                EndDate = w.CreatedAt.AddDays(30),
                Status = w.Active ? "active" : "expired",
                MaxDrivers = w.MaximumDriverCount,
                MaxRoutes = 100,
                MaxCustomers = 1000
            }
        }).ToList();
    }

    private static string GetPlanType(DateTime createdAt)
    {
        var daysOld = (DateTime.UtcNow - createdAt).Days;
        return daysOld <= 30 ? "trial" : "basic";
    }
}