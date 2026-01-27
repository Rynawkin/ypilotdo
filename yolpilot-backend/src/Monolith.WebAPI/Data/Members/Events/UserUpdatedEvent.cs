// src/Monolith.WebAPI/Data/Members/Events/UserUpdatedEvent.cs

using MediatR;
using Monolith.WebAPI.Applications.Events;

namespace Monolith.WebAPI.Data.Members.Events;

public class UserUpdatedEvent : NotificationEvent
{
    public Guid UserId { get; set; }
    public string? UpdatedFields { get; set; }
    
    public UserUpdatedEvent(Guid userId, string? updatedFields = null)
    {
        UserId = userId;
        UpdatedFields = updatedFields;
    }
}

public class UserUpdatedEventHandler : INotificationHandler<UserUpdatedEvent>
{
    public Task Handle(UserUpdatedEvent notification, CancellationToken cancellationToken)
    {
        // Handle the event
        return Task.CompletedTask;
    }
}