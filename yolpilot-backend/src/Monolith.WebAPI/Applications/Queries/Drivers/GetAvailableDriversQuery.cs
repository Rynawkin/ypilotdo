// src/Monolith.WebAPI/Applications/Queries/Drivers/GetAvailableDriversQuery.cs

using MediatR;
using Microsoft.EntityFrameworkCore;
using Monolith.WebAPI.Applications.Commands;
using Monolith.WebAPI.Applications.Commands.Drivers;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Responses.Workspace;
using Monolith.WebAPI.Services.Members;

namespace Monolith.WebAPI.Applications.Queries.Drivers;

public class GetAvailableDriversQuery : BaseAuthenticatedCommand<List<DriverResponse>>
{
}

public class GetAvailableDriversQueryHandler : BaseAuthenticatedCommandHandler<GetAvailableDriversQuery, List<DriverResponse>>
{
    private readonly AppDbContext _context;

    public GetAvailableDriversQueryHandler(AppDbContext context, IUserService userService) : base(userService)
    {
        _context = context;
    }

    protected override async Task<List<DriverResponse>> HandleCommand(GetAvailableDriversQuery request, CancellationToken cancellationToken)
    {
        var drivers = await _context.Set<Data.Workspace.Driver>()
            .Where(d => d.WorkspaceId == User.WorkspaceId && d.Status == "available")
            .OrderBy(d => d.Name)
            .ToListAsync(cancellationToken);

        return drivers.Select(d => d.ToResponse()).ToList();
    }
}