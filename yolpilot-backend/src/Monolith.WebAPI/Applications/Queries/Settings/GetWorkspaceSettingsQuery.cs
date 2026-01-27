// src/Monolith.WebAPI/Applications/Queries/Settings/GetWorkspaceSettingsQuery.cs

using Microsoft.EntityFrameworkCore;
using Monolith.WebAPI.Applications.Commands;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Responses.Settings;
using Monolith.WebAPI.Services.Members;

namespace Monolith.WebAPI.Applications.Queries.Settings;

public class GetWorkspaceSettingsQuery : BaseAuthenticatedCommand<WorkspaceSettingsResponse>
{
}

public class GetWorkspaceSettingsQueryHandler : BaseAuthenticatedCommandHandler<GetWorkspaceSettingsQuery, WorkspaceSettingsResponse>
{
    private readonly AppDbContext _context;

    public GetWorkspaceSettingsQueryHandler(IUserService userService, AppDbContext context) 
        : base(userService)
    {
        _context = context;
    }

    protected override async Task<WorkspaceSettingsResponse> HandleCommand(GetWorkspaceSettingsQuery request, CancellationToken cancellationToken)
    {
        var workspace = await _context.Workspaces
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == User.WorkspaceId, cancellationToken);

        if (workspace == null)
            throw new Exception("Workspace not found");

        return new WorkspaceSettingsResponse
        {
            Name = workspace.Name,
            Logo = null, // TODO: Implement logo storage
            Address = workspace.Address,
            City = workspace.City,
            PostalCode = workspace.PostalCode,
            TaxNumber = workspace.TaxNumber,
            PhoneNumber = workspace.PhoneNumber,
            Email = workspace.Email,
            Website = workspace.Website,
            Currency = workspace.Currency,
            TimeZone = workspace.TimeZone,
            Language = workspace.Language,
            DateFormat = workspace.DateFormat ?? "DD/MM/YYYY",
            FirstDayOfWeek = workspace.FirstDayOfWeek ?? "monday"
        };
    }
}