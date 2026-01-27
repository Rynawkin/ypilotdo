using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Monolith.WebAPI.Extensions;

namespace Monolith.WebAPI.Hubs;

[Authorize]
public class NotificationHub : Hub
{
    private readonly ILogger<NotificationHub> _logger;

    public NotificationHub(ILogger<NotificationHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.GetId();
        if (userId.HasValue)
        {
            // Join user to their personal notification group
            await Groups.AddToGroupAsync(Context.ConnectionId, $"User_{userId}");
            _logger.LogInformation("User {UserId} connected to NotificationHub with connection {ConnectionId}", userId, Context.ConnectionId);
        }
        
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.User?.GetId();
        if (userId.HasValue)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"User_{userId}");
            _logger.LogInformation("User {UserId} disconnected from NotificationHub", userId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    // Client can call this method to join workspace notifications group
    public async Task JoinWorkspaceGroup(int workspaceId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"Workspace_{workspaceId}");
        _logger.LogInformation("Connection {ConnectionId} joined workspace group {WorkspaceId}", Context.ConnectionId, workspaceId);
    }

    public async Task LeaveWorkspaceGroup(int workspaceId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Workspace_{workspaceId}");
        _logger.LogInformation("Connection {ConnectionId} left workspace group {WorkspaceId}", Context.ConnectionId, workspaceId);
    }

    // Client can call this to confirm notification received
    public async Task ConfirmNotificationReceived(int notificationId)
    {
        var userId = Context.User?.GetId();
        _logger.LogInformation("User {UserId} confirmed receipt of notification {NotificationId}", userId, notificationId);
        
        // Here you could update delivery status if needed
        await Clients.Caller.SendAsync("NotificationConfirmed", notificationId);
    }
}