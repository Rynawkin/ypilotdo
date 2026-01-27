// src/Monolith.WebAPI/Data/Workspace/Events/SavedLocationCreatedEvent.cs

using MediatR;
using Monolith.WebAPI.Applications.Events;

namespace Monolith.WebAPI.Data.Workspace.Events;

public class SavedLocationCreatedEvent : NotificationEvent
{
    public int LocationId { get; set; }
    public string Name { get; set; } = string.Empty;
    
    public SavedLocationCreatedEvent(int locationId, string name = "")
    {
        LocationId = locationId;
        Name = name ?? string.Empty;
    }
}

public class SavedLocationUpdatedEvent : NotificationEvent
{
    public int LocationId { get; set; }
    public string Name { get; set; } = string.Empty;
    
    public SavedLocationUpdatedEvent(int locationId, string name = "")
    {
        LocationId = locationId;
        Name = name ?? string.Empty;
    }
}

public class SavedLocationCreatedEventHandler : INotificationHandler<SavedLocationCreatedEvent>
{
    public Task Handle(SavedLocationCreatedEvent notification, CancellationToken cancellationToken)
    {
        // Handle the event
        return Task.CompletedTask;
    }
}

public class SavedLocationUpdatedEventHandler : INotificationHandler<SavedLocationUpdatedEvent>
{
    public Task Handle(SavedLocationUpdatedEvent notification, CancellationToken cancellationToken)
    {
        // Handle the event
        return Task.CompletedTask;
    }
}