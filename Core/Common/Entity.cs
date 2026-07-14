namespace Core.Common;

/// <summary>Base entity with optimistic concurrency and domain-event collection.</summary>
public abstract class Entity
{
    private readonly List<IDomainEvent> domainEvents = [];
    /// <summary>Gets the entity identifier.</summary>
    public Guid Id { get; protected init; } = Guid.NewGuid();
    /// <summary>Gets the EF Core concurrency token.</summary>
    public uint Version { get; private set; }
    /// <summary>Gets events raised by this aggregate.</summary>
    public IReadOnlyCollection<IDomainEvent> DomainEvents => domainEvents;
    /// <summary>Adds a domain event for dispatch after persistence.</summary>
    protected void Raise(IDomainEvent domainEvent) => domainEvents.Add(domainEvent);
    /// <summary>Clears captured events after they are written to the outbox.</summary>
    public void ClearDomainEvents() => domainEvents.Clear();
}

/// <summary>Marks a domain fact that must be dispatched reliably.</summary>
public interface IDomainEvent { Guid EventId { get; } DateTimeOffset OccurredAt { get; } }
