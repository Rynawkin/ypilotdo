using Monolith.WebAPI.Applications.Events;

namespace Monolith.WebAPI.Data;

public abstract class BaseEntity
{
    private readonly List<NotificationEvent> _domainEvents = new();
    
    // Constructor EKLENDI - Tarih sorununu çözer
    protected BaseEntity()
    {
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        IsDeleted = false;
    }
    
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    
    public IReadOnlyCollection<NotificationEvent> DomainEvents => _domainEvents.AsReadOnly();
    
    public void AddDomainEvent(NotificationEvent eventItem)
    {
        _domainEvents.Add(eventItem);
    }
    
    public void RemoveDomainEvent(NotificationEvent eventItem)
    {
        _domainEvents.Remove(eventItem);
    }
    
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}