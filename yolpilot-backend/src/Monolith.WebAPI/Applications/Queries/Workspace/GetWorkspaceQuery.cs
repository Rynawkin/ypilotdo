using MediatR;
using Microsoft.EntityFrameworkCore;
using Monolith.WebAPI.Applications.Commands;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Infrastructure;
using Monolith.WebAPI.Responses.Workspace;
using Monolith.WebAPI.Services.Members;

namespace Monolith.WebAPI.Applications.Queries.Workspace;

public class GetWorkspaceQuery : BaseAuthenticatedCommand<WorkspaceResponse>
{
    // ✅ Sadece Admin ve üstü roller workspace detaylarını görebilir
    public override bool RequiresAdmin => true;
}

public class GetWorkspaceQueryHandler : BaseAuthenticatedCommandHandler<GetWorkspaceQuery, WorkspaceResponse>
{
    private readonly AppDbContext _context;

    public GetWorkspaceQueryHandler(AppDbContext context, IUserService userService) 
        : base(userService)
    {
        _context = context;
    }

    protected override async Task<WorkspaceResponse> HandleCommand(GetWorkspaceQuery request, CancellationToken cancellationToken)
    {
        // ✅ Kullanıcının kendi workspace'ini getir
        var workspace = await _context.Workspaces
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == User.WorkspaceId, cancellationToken);

        if (workspace == null) 
            throw new ApiException("Workspace not found.", 404);

        return new WorkspaceResponse(workspace);
    }
}