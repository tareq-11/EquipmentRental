using Core.Common;

namespace infrastructure.Persistence;

/// <summary>Durable integration event awaiting a retry-safe processor in a future milestone.</summary>
public sealed class OutboxMessage
{
    /// <summary>Gets the message identifier.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();
    /// <summary>Gets the stable event identifier.</summary>
    public Guid EventId { get; init; }
    /// <summary>Gets the event type name.</summary>
    public string Type { get; init; } = string.Empty;
    /// <summary>Gets serialized event data.</summary>
    public string Payload { get; init; } = string.Empty;
    /// <summary>Gets when the message was created.</summary>
    public DateTimeOffset OccurredAt { get; init; }
    /// <summary>Gets when delivery completed.</summary>
    public DateTimeOffset? ProcessedAt { get; set; }
    /// <summary>Gets delivery attempts.</summary>
    public int AttemptCount { get; set; }
    /// <summary>Gets the EF concurrency token.</summary>
    public uint Version { get; private set; }
}

/// <summary>Stores a command key and prior response to prevent duplicate effects.</summary>
public sealed class IdempotentRequest
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Key { get; init; } = string.Empty;
    public string RequestName { get; init; } = string.Empty;
    public string ActorScope { get; init; } = string.Empty;
    public string RequestFingerprint { get; init; } = string.Empty;
    public string ResponseJson { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; init; }
    public uint Version { get; private set; }
}

/// <summary>Captures a sensitive action without storing secrets.</summary>
public sealed class AuditLog
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid? ActingUserId { get; init; }
    public string ActorType { get; init; } = string.Empty;
    public string Action { get; init; } = string.Empty;
    public string TargetType { get; init; } = string.Empty;
    public string TargetId { get; init; } = string.Empty;
    public DateTimeOffset OccurredAt { get; init; }
    public string? OldValues { get; init; }
    public string? NewValues { get; init; }
    public string? IpAddress { get; init; }
    public string? Reason { get; init; }
    public uint Version { get; private set; }
}
