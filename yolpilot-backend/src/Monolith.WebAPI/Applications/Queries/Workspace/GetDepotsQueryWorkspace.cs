using MediatR;
using Microsoft.EntityFrameworkCore;
using Monolith.WebAPI.Applications.Commands;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Infrastructure;
using Monolith.WebAPI.Services.Members;
using Monolith.WebAPI.Responses.Workspace;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Monolith.WebAPI.Applications.Queries.Workspace;

public class GetDepotsQueryWorkspace : BaseAuthenticatedCommand<List<DepotResponse>>
{
    public string SearchTerm { get; set; }
    
    // ✅ Dispatcher ve üstü roller depot yönetimi yapabilir
    public override bool RequiresDispatcher => true;
}

public class GetDepotsQueryWorkspaceHandler : BaseAuthenticatedCommandHandler<GetDepotsQueryWorkspace, List<DepotResponse>>
{
    private readonly AppDbContext _context;
    
    public GetDepotsQueryWorkspaceHandler(AppDbContext context, IUserService userService) 
        : base(userService)
    {
        _context = context;
    }
    
    protected override async Task<List<DepotResponse>> HandleCommand(GetDepotsQueryWorkspace request, CancellationToken cancellationToken)
    {
        // ✅ Driver bu noktaya gelemez çünkü RequiresDispatcher = true
        
        var query = _context.Depots
            .Where(d => d.WorkspaceId == User.WorkspaceId);
        
        // Arama filtresi
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var searchTerm = request.SearchTerm.ToLower();
            query = query.Where(d => 
                d.Name.ToLower().Contains(searchTerm) ||
                d.Address.ToLower().Contains(searchTerm));
        }
        
        var depots = await query
            .OrderByDescending(d => d.IsDefault)
            .ThenBy(d => d.Name)
            .ToListAsync(cancellationToken);
        
        return depots.Select(d => new DepotResponse(d)).ToList();
    }
}

// ✅ Driver için kendi depot bilgisini görme
public class GetMyDepotQuery : BaseAuthenticatedCommand<DepotResponse>
{
    // ✅ Herkes kendi depot bilgisini görebilir
    public override bool RequiresDriver => false;
}

public class GetMyDepotQueryHandler : BaseAuthenticatedCommandHandler<GetMyDepotQuery, DepotResponse>
{
    private readonly AppDbContext _context;
    
    public GetMyDepotQueryHandler(AppDbContext context, IUserService userService) 
        : base(userService)
    {
        _context = context;
    }
    
    protected override async Task<DepotResponse> HandleCommand(GetMyDepotQuery request, CancellationToken cancellationToken)
    {
        // Driver'ın atanmış depot'u varsa getir
        if (!User.DepotId.HasValue)
            throw new ApiException("You are not assigned to any depot.", 404);
            
        var depot = await _context.Depots
            .FirstOrDefaultAsync(d => d.Id == User.DepotId.Value && d.WorkspaceId == User.WorkspaceId, cancellationToken);
            
        if (depot == null)
            throw new ApiException("Depot not found.", 404);
            
        return new DepotResponse(depot);
    }
}