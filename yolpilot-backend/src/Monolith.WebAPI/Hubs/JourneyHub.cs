// src/Monolith.WebAPI/Hubs/JourneyHub.cs
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;

namespace Monolith.WebAPI.Hubs
{
    [Authorize]
    public class JourneyHub : Hub
    {
        private readonly ILogger<JourneyHub> _logger;
        
        public JourneyHub(ILogger<JourneyHub> logger)
        {
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = Context.UserIdentifier;
            var workspaceId = Context.User?.FindFirst("WorkspaceId")?.Value;
            
            if (!string.IsNullOrEmpty(workspaceId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"workspace-{workspaceId}");
                await Groups.AddToGroupAsync(Context.ConnectionId, $"user-{userId}");
                _logger.LogInformation("User {UserId} connected to workspace {WorkspaceId}", userId, workspaceId);
            }
            
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            _logger.LogInformation("User disconnected: {ConnectionId}", Context.ConnectionId);
            await base.OnDisconnectedAsync(exception);
        }

        public async Task JoinJourneyGroup(int journeyId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"journey-{journeyId}");
            _logger.LogInformation("Connection {ConnectionId} joined journey-{JourneyId}", Context.ConnectionId, journeyId);
        }

        public async Task LeaveJourneyGroup(int journeyId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"journey-{journeyId}");
            _logger.LogInformation("Connection {ConnectionId} left journey-{JourneyId}", Context.ConnectionId, journeyId);
        }
    }
}