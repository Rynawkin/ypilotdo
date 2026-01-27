// src/Monolith.WebAPI/Applications/Queries/Drivers/GetDriverQuery.cs

using MediatR;
using Microsoft.EntityFrameworkCore;
using Monolith.WebAPI.Applications.Commands;
using Monolith.WebAPI.Applications.Commands.Drivers;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Responses.Workspace;
using Monolith.WebAPI.Services.Members;

namespace Monolith.WebAPI.Applications.Queries.Drivers;

public class GetDriverQuery : BaseAuthenticatedCommand<DriverResponse>
{
    public int Id { get; set; }
}

public class GetDriverQueryHandler : BaseAuthenticatedCommandHandler<GetDriverQuery, DriverResponse>
{
    private readonly AppDbContext _context;

    public GetDriverQueryHandler(AppDbContext context, IUserService userService) : base(userService)
    {
        _context = context;
    }

    protected override async Task<DriverResponse> HandleCommand(GetDriverQuery request, CancellationToken cancellationToken)
    {
        var driver = await _context.Set<Data.Workspace.Driver>()
            .Include(d => d.Vehicle)
            .FirstOrDefaultAsync(d => d.Id == request.Id && d.WorkspaceId == User.WorkspaceId, cancellationToken);

        if (driver == null)
        {
            throw new KeyNotFoundException($"Sürücü bulunamadı: {request.Id}");
        }

        return driver.ToResponse();
    }
}