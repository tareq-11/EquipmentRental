using Core.Foundation;
using Microsoft.EntityFrameworkCore;
using Services.Foundation;

namespace infrastructure.Persistence;

/// <summary>EF-backed store for the deliberately narrow foundation demonstration command.</summary>
public sealed class FoundationProbeStore(EquipmentRentalDbContext dbContext) : IFoundationProbeStore
{
    public Task<FoundationProbe?> GetAsync(Guid id, CancellationToken cancellationToken) => dbContext.FoundationProbes.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
    public void SetExpectedVersion(FoundationProbe probe, uint version) => dbContext.Entry(probe).Property(x => x.Version).OriginalValue = version;
    public void AddAudit(Guid? actingUserId, string actorType, string action, string targetType, string targetId, string? ipAddress, string? reason, DateTimeOffset occurredAt)
    {
        if (action.Contains("override", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(reason)) throw new InvalidOperationException("Manual overrides require a reason.");
        dbContext.AuditLogs.Add(new() { ActingUserId = actingUserId, ActorType = actorType, Action = action, TargetType = targetType, TargetId = targetId, IpAddress = ipAddress, Reason = reason, OccurredAt = occurredAt, NewValues = "{\"source\":\"foundation\"}" });
    }
}
