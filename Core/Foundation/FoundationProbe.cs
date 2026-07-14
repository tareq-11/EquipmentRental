using Core.Common;

namespace Core.Foundation;

/// <summary>A non-business aggregate used only to demonstrate cross-cutting foundations.</summary>
public sealed class FoundationProbe : Entity
{
    private FoundationProbe() { }
    private FoundationProbe(string label, DateTimeOffset createdAt)
    {
        Label = label;
        CreatedAt = createdAt;
        Raise(new FoundationProbeCreated(Id, label, createdAt));
    }

    /// <summary>Gets the non-business label.</summary>
    public string Label { get; private set; } = string.Empty;
    /// <summary>Gets the creation time.</summary>
    public DateTimeOffset CreatedAt { get; private set; }
    /// <summary>Creates a demonstrative aggregate.</summary>
    public static FoundationProbe Create(string label, TimeProvider clock) => new(label, clock.GetUtcNow());
    /// <summary>Changes the probe label for the development-only concurrency demonstration.</summary>
    public void Rename(string label) => Label = label;
}

/// <summary>Records foundation-probe creation for the persistent outbox.</summary>
public sealed record FoundationProbeCreated(Guid ProbeId, string Label, DateTimeOffset OccurredAt) : IDomainEvent
{
    /// <summary>Gets the unique event identifier.</summary>
    public Guid EventId { get; } = Guid.NewGuid();
}
