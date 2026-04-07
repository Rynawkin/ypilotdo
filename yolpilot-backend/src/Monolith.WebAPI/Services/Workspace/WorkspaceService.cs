using Monolith.WebAPI.Data;
using Monolith.WebAPI.Services.Subscription;

namespace Monolith.WebAPI.Services.Workspace;

public interface IWorkspaceService
{
    Task ThrowExceptionIfReachedToMaxDriverCountAsync(int workspaceId, CancellationToken cancellationToken);
}

public class WorkspaceService(AppDbContext context, ISubscriptionService subscriptionService) : IWorkspaceService
{
    public async Task ThrowExceptionIfReachedToMaxDriverCountAsync(int workspaceId, CancellationToken cancellationToken)
    {
        await subscriptionService.EnsureDriverLimitNotExceeded(workspaceId);
    }
}
