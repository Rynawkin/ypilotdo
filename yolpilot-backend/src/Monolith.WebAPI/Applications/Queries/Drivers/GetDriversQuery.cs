using MediatR;
using Microsoft.EntityFrameworkCore;
using Monolith.WebAPI.Applications.Commands;
using Monolith.WebAPI.Applications.Commands.Drivers;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Responses.Workspace;
using Monolith.WebAPI.Services.Members;

namespace Monolith.WebAPI.Applications.Queries.Drivers;

public class GetDriversQuery : BaseAuthenticatedCommand<List<DriverResponse>>
{
    public string? Status { get; set; }
    public string? SearchQuery { get; set; }
    
    // ✅ Dispatcher ve üstü roller erişebilir
    public override bool RequiresDispatcher => true;
}

public class GetDriversQueryHandler : BaseAuthenticatedCommandHandler<GetDriversQuery, List<DriverResponse>>
{
    private readonly AppDbContext _context;

    public GetDriversQueryHandler(AppDbContext context, IUserService userService) : base(userService)
    {
        _context = context;
    }

    protected override async Task<List<DriverResponse>> HandleCommand(GetDriversQuery request, CancellationToken cancellationToken)
    {
        // ✅ Driver bu noktaya gelemez çünkü RequiresDispatcher = true
        
        var query = _context.Set<Data.Workspace.Driver>()
            .Where(d => d.WorkspaceId == User.WorkspaceId);

        // Apply status filter
        if (!string.IsNullOrEmpty(request.Status) && request.Status != "all")
        {
            query = query.Where(d => d.Status == request.Status);
        }

        // Apply search filter
        if (!string.IsNullOrEmpty(request.SearchQuery))
        {
            var searchLower = request.SearchQuery.ToLower();
            query = query.Where(d => 
                d.Name.ToLower().Contains(searchLower) ||
                d.Phone.ToLower().Contains(searchLower) ||
                d.LicenseNumber.ToLower().Contains(searchLower) ||
                (d.Email != null && d.Email.ToLower().Contains(searchLower)));
        }

        var drivers = await query
            .OrderBy(d => d.Name)
            .ToListAsync(cancellationToken);

        return drivers.Select(d => d.ToResponse()).ToList();
    }
}