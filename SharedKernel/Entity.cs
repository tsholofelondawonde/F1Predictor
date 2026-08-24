namespace SharedKernel;

/// <summary>
/// Base entity type providing identity and domain event support.
/// Identity is the single source of truth for aggregates and child entities.
/// </summary>
public abstract class Entity
{
    /// <summary>
    /// Primary key identifier for the entity. Protected setter prevents
    /// external mutation while allowing EF Core and derived types to set it.
    /// </summary>
    public Guid Id { get; protected set; }

    private readonly List<IDomainEvent> _domainEvents = [];

    public List<IDomainEvent> DomainEvents => [.. _domainEvents];

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    public void Raise(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }
}