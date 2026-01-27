using Microsoft.EntityFrameworkCore;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Infrastructure;

namespace Monolith.WebAPI.Services.Workspace;

public interface IWorkspaceService
{
    Task ThrowExceptionIfReachedToMaxDriverCountAsync(int workspaceId, CancellationToken cancellationToken);
}

public class WorkspaceService(AppDbContext context) : IWorkspaceService
{
    public async Task ThrowExceptionIfReachedToMaxDriverCountAsync(int workspaceId, CancellationToken cancellationToken)
    {
        var workspace = await context.Workspaces
            .Where(x => x.Id == workspaceId)
            .Select(x => new
            {
                x.MaximumDriverCount,
                DriverCount = context.Users.Count(u => u.WorkspaceId == workspaceId && u.IsDriver)
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (workspace.DriverCount >= workspace.MaximumDriverCount)
            throw new ApiException("You have reached the maximum driver count", 400);
    }
}