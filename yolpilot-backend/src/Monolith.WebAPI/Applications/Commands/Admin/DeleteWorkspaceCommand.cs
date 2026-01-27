// src/Monolith.WebAPI/Applications/Commands/Admin/DeleteWorkspaceCommand.cs
using MediatR;
using Microsoft.EntityFrameworkCore;
using Monolith.WebAPI.Applications.Commands;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Services.Members;

namespace Monolith.WebAPI.Applications.Commands.Admin;

public class DeleteWorkspaceCommand : BaseAuthenticatedCommand<bool>
{
    public int WorkspaceId { get; set; }
    
    public override bool RequiresSuperAdmin => true;
}

public class DeleteWorkspaceCommandHandler(IUserService userService, AppDbContext context) 
    : BaseAuthenticatedCommandHandler<DeleteWorkspaceCommand, bool>(userService)
{
    private readonly AppDbContext _context = context;

    protected override async Task<bool> HandleCommand(DeleteWorkspaceCommand request, CancellationToken cancellationToken)
    {
        var workspace = await _context.Workspaces
            .FirstOrDefaultAsync(w => w.Id == request.WorkspaceId, cancellationToken);

        if (workspace == null)
            return false;

        // Soft delete - workspace'i pasif yap
        var activeProperty = typeof(Data.Workspace.Workspace).GetProperty(nameof(Data.Workspace.Workspace.Active));
        activeProperty?.SetValue(workspace, false);

        workspace.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}