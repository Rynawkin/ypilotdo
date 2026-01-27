// src/Monolith.WebAPI/Applications/Events/NotificationEvent.cs

using MediatR;

namespace Monolith.WebAPI.Applications.Events;

public abstract class NotificationEvent : INotification
{
    public DateTime Timestamp { get; protected set; }
    public bool AfterSavingChanges { get; protected set; }
    
    protected NotificationEvent()
    {
        Timestamp = DateTime.UtcNow;
        AfterSavingChanges = false;
    }
}