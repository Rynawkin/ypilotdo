// src/Monolith.WebAPI/Hubs/JourneyHub.cs
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Monolith.WebAPI.Data;

namespace Monolith.WebAPI.Hubs
{
    [Authorize]
    public class JourneyHub : Hub
    {
        private readonly ILogger<JourneyHub> _logger;
        private readonly AppDbContext _context;
        
        public JourneyHub(ILogger<JourneyHub> logger, AppDbContext context)
        {
            _logger = logger;
            _context = context;
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
            var workspaceId = GetWorkspaceIdOrThrow();
            var exists = await _context.Journeys
                .AsNoTracking()
                .AnyAsync(j => j.Id == journeyId && j.WorkspaceId == workspaceId);

            if (!exists)
            {
                throw new HubException("Journey not found.");
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, $"journey-{journeyId}");
            _logger.LogInformation("Connection {ConnectionId} joined journey-{JourneyId}", Context.ConnectionId, journeyId);
        }

        public async Task LeaveJourneyGroup(int journeyId)
        {
            var workspaceId = GetWorkspaceIdOrThrow();
            var exists = await _context.Journeys
                .AsNoTracking()
                .AnyAsync(j => j.Id == journeyId && j.WorkspaceId == workspaceId);

            if (!exists)
            {
                throw new HubException("Journey not found.");
            }

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"journey-{journeyId}");
            _logger.LogInformation("Connection {ConnectionId} left journey-{JourneyId}", Context.ConnectionId, journeyId);
        }

        private int GetWorkspaceIdOrThrow()
        {
            var workspaceId = Context.User?.FindFirst("WorkspaceId")?.Value;
            if (string.IsNullOrWhiteSpace(workspaceId) || !int.TryParse(workspaceId, out var parsedWorkspaceId))
            {
                throw new HubException("Workspace claim is missing.");
            }

            return parsedWorkspaceId;
        }
    }
}
