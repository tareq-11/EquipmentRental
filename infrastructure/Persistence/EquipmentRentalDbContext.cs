using Microsoft.EntityFrameworkCore;
using Core.Common;
using Core.Foundation;
using System.Text.Json;

namespace infrastructure.Persistence;

/// <summary>
/// Represents the persistence boundary for the Equipment Rental application.
/// Domain entities are introduced in subsequent milestones.
/// </summary>
public sealed class EquipmentRentalDbContext(DbContextOptions<EquipmentRentalDbContext> options) : DbContext(options)
{
    /// <summary>Gets foundation probes used solely to manually exercise application infrastructure.</summary>
    public DbSet<FoundationProbe> FoundationProbes => Set<FoundationProbe>();
    /// <summary>Gets pending durable domain events.</summary>
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    /// <summary>Gets duplicate-sensitive command records.</summary>
    public DbSet<IdempotentRequest> IdempotentRequests => Set<IdempotentRequest>();
    /// <summary>Gets immutable audit records.</summary>
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FoundationProbe>(builder => { builder.ToTable("foundation_probes"); builder.HasKey(x => x.Id); builder.Property(x => x.Label).HasMaxLength(100).IsRequired(); builder.Property(x => x.Version).IsRowVersion(); });
        modelBuilder.Entity<OutboxMessage>(builder => { builder.ToTable("outbox_messages"); builder.HasKey(x => x.Id); builder.HasIndex(x => new { x.ProcessedAt, x.OccurredAt }); builder.HasIndex(x => x.EventId).IsUnique(); builder.Property(x => x.Type).HasMaxLength(500); builder.Property(x => x.Version).IsRowVersion(); });
        modelBuilder.Entity<IdempotentRequest>(builder => { builder.ToTable("idempotent_requests"); builder.HasKey(x => x.Id); builder.HasIndex(x => new { x.Key, x.RequestName, x.ActorScope }).IsUnique(); builder.Property(x => x.Key).HasMaxLength(200); builder.Property(x => x.RequestName).HasMaxLength(300); builder.Property(x => x.ActorScope).HasMaxLength(300); builder.Property(x => x.RequestFingerprint).HasMaxLength(64); builder.Property(x => x.Version).IsRowVersion(); });
        modelBuilder.Entity<AuditLog>(builder => { builder.ToTable("audit_logs"); builder.HasKey(x => x.Id); builder.HasIndex(x => new { x.TargetType, x.TargetId, x.OccurredAt }); builder.HasIndex(x => x.OccurredAt); builder.Property(x => x.ActorType).HasMaxLength(30).IsRequired(); builder.Property(x => x.Action).HasMaxLength(200); builder.Property(x => x.TargetType).HasMaxLength(200); builder.Property(x => x.TargetId).HasMaxLength(200); builder.Property(x => x.Reason).HasMaxLength(1000); builder.Property(x => x.Version).IsRowVersion(); });
    }

    /// <inheritdoc />
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var aggregates = ChangeTracker.Entries<Entity>().Select(x => x.Entity).Where(x => x.DomainEvents.Count > 0).ToArray();
        var stagedOutbox = new List<OutboxMessage>();
        foreach (var aggregate in aggregates)
        {
            foreach (var domainEvent in aggregate.DomainEvents)
            {
                var message = new OutboxMessage { EventId = domainEvent.EventId, Type = domainEvent.GetType().FullName!, Payload = JsonSerializer.Serialize(domainEvent, domainEvent.GetType()), OccurredAt = domainEvent.OccurredAt };
                stagedOutbox.Add(message);
                OutboxMessages.Add(message);
            }
        }
        try
        {
            var saved = await base.SaveChangesAsync(cancellationToken);
            foreach (var aggregate in aggregates) aggregate.ClearDomainEvents();
            return saved;
        }
        catch
        {
            // Keep aggregate events for a later retry without leaving duplicate staged messages tracked.
            foreach (var message in stagedOutbox) Entry(message).State = EntityState.Detached;
            throw;
        }
    }
}
