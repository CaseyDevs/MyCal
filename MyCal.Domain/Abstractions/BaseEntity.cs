namespace MyCal.Domain.Abstractions;

public abstract class BaseEntity : IDomainEvent
{
    private readonly List<IDomainEvent> _domainEvents = [];
    
    public int Id { get; init; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public IReadOnlyList<IDomainEvent> GetDomainEvents => _domainEvents.AsReadOnly();
    public void RaiseDomainEvent(IDomainEvent @event) => _domainEvents.Add(@event);
    public void ClearDomainEvents() => _domainEvents.Clear();
}