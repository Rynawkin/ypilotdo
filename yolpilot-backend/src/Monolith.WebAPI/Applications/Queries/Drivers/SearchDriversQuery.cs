// src/Monolith.WebAPI/Applications/Queries/Drivers/SearchDriversQuery.cs

using MediatR;
using Microsoft.EntityFrameworkCore;
using Monolith.WebAPI.Applications.Commands;
using Monolith.WebAPI.Applications.Commands.Drivers;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Responses.Workspace;
using Monolith.WebAPI.Services.Members;

namespace Monolith.WebAPI.Applications.Queries.Drivers;

public class SearchDriversQuery : BaseAuthenticatedCommand<List<DriverResponse>>
{
    public string Query { get; set; } = string.Empty;
}

public class SearchDriversQueryHandler : BaseAuthenticatedCommandHandler<SearchDriversQuery, List<DriverResponse>>
{
    private readonly AppDbContext _context;

    public SearchDriversQueryHandler(AppDbContext context, IUserService userService) : base(userService)
    {
        _context = context;
    }

    protected override async Task<List<DriverResponse>> HandleCommand(SearchDriversQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Query))
        {
            return new List<DriverResponse>();
        }

        var searchLower = request.Query.ToLower();
        
        var drivers = await _context.Set<Data.Workspace.Driver>()
            .Where(d => d.WorkspaceId == User.WorkspaceId &&
                       (d.Name.ToLower().Contains(searchLower) ||
                        d.Phone.ToLower().Contains(searchLower) ||
                        d.LicenseNumber.ToLower().Contains(searchLower) ||
                        (d.Email != null && d.Email.ToLower().Contains(searchLower))))
            .OrderBy(d => d.Name)
            .Take(20) // Limit results for performance
            .ToListAsync(cancellationToken);

        return drivers.Select(d => d.ToResponse()).ToList();
    }
}