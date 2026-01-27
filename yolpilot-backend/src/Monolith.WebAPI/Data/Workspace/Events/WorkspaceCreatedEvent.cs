// src/Monolith.WebAPI/Data/Workspace/Events/WorkspaceCreatedEvent.cs

using MediatR;
using Monolith.WebAPI.Applications.Events;

namespace Monolith.WebAPI.Data.Workspace.Events;

public class WorkspaceCreatedEvent : NotificationEvent
{
    public int WorkspaceId { get; set; }
    public string WorkspaceName { get; set; } = string.Empty;
    public Guid CreatedByUserId { get; set; }
    
    public WorkspaceCreatedEvent(int workspaceId, string workspaceName = "", Guid? createdByUserId = null)
    {
        WorkspaceId = workspaceId;
        WorkspaceName = workspaceName ?? string.Empty;
        CreatedByUserId = createdByUserId ?? Guid.Empty;
    }
}