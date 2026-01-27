// src/Monolith.WebAPI/Data/Members/Events/MemberJoinedEvent.cs

using MediatR;
using Monolith.WebAPI.Applications.Events;

namespace Monolith.WebAPI.Data.Members.Events;

public class MemberJoinedEvent : NotificationEvent
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int WorkspaceId { get; set; }
    public DateTime JoinedAt { get; set; }
    
    public MemberJoinedEvent(Guid userId, string email, string name, int workspaceId = 0)
    {
        UserId = userId;
        Email = email;
        Name = name;
        WorkspaceId = workspaceId;
        JoinedAt = DateTime.UtcNow;
    }
}

public class MemberJoinedEventHandler : INotificationHandler<MemberJoinedEvent>
{
    public Task Handle(MemberJoinedEvent notification, CancellationToken cancellationToken)
    {
        // Handle the event
        return Task.CompletedTask;
    }
}