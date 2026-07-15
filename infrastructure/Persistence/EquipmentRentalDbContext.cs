using Microsoft.EntityFrameworkCore;
using Core.Common;
using Core.Foundation;
using System.Text.Json;
using Core.Identity;

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
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<User> Users => Set<User>();
    public DbSet<CustomerProfile> CustomerProfiles => Set<CustomerProfile>();
    public DbSet<EmployeeProfile> EmployeeProfiles => Set<EmployeeProfile>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<OtpCode> OtpCodes => Set<OtpCode>();
    public DbSet<Notification> Notifications => Set<Notification>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FoundationProbe>(builder => { builder.ToTable("foundation_probes"); builder.HasKey(x => x.Id); builder.Property(x => x.Label).HasMaxLength(100).IsRequired(); builder.Property(x => x.Version).IsRowVersion(); });
        modelBuilder.Entity<OutboxMessage>(builder => { builder.ToTable("outbox_messages"); builder.HasKey(x => x.Id); builder.HasIndex(x => new { x.ProcessedAt, x.NextAttemptAt, x.OccurredAt }); builder.HasIndex(x => x.EventId).IsUnique(); builder.Property(x => x.Type).HasMaxLength(500); builder.Property(x => x.Version).IsRowVersion(); });
        modelBuilder.Entity<IdempotentRequest>(builder => { builder.ToTable("idempotent_requests"); builder.HasKey(x => x.Id); builder.HasIndex(x => new { x.Key, x.RequestName, x.ActorScope }).IsUnique(); builder.Property(x => x.Key).HasMaxLength(200); builder.Property(x => x.RequestName).HasMaxLength(300); builder.Property(x => x.ActorScope).HasMaxLength(300); builder.Property(x => x.RequestFingerprint).HasMaxLength(64); builder.Property(x => x.Version).IsRowVersion(); });
        modelBuilder.Entity<AuditLog>(builder => { builder.ToTable("audit_logs"); builder.HasKey(x => x.Id); builder.HasIndex(x => new { x.TargetType, x.TargetId, x.OccurredAt }); builder.HasIndex(x => x.OccurredAt); builder.Property(x => x.ActorType).HasMaxLength(30).IsRequired(); builder.Property(x => x.Action).HasMaxLength(200); builder.Property(x => x.TargetType).HasMaxLength(200); builder.Property(x => x.TargetId).HasMaxLength(200); builder.Property(x => x.Reason).HasMaxLength(1000); builder.Property(x => x.Version).IsRowVersion(); });
        modelBuilder.Entity<Organization>(builder => { builder.ToTable("organizations"); builder.HasKey(x => x.Id); builder.HasIndex(x => x.Name).IsUnique(); builder.Property(x => x.Name).HasMaxLength(200).IsRequired(); builder.Property(x => x.Version).IsRowVersion(); });
        modelBuilder.Entity<User>(builder =>
        {
            builder.ToTable("users"); builder.HasKey(x => x.Id); builder.HasIndex(x => x.Email).IsUnique(); builder.HasIndex(x => new { x.OrganizationId, x.Role, x.AccountStatus });
            builder.Property(x => x.FullName).HasMaxLength(200).IsRequired(); builder.Property(x => x.Email).HasMaxLength(320).IsRequired(); builder.Property(x => x.PhoneNumber).HasMaxLength(20).IsRequired(); builder.Property(x => x.PasswordHash).HasMaxLength(200).IsRequired(); builder.Property(x => x.SecurityStamp).HasMaxLength(64).IsRequired(); builder.Property(x => x.Role).HasConversion<string>().HasMaxLength(30); builder.Property(x => x.AccountStatus).HasConversion<string>().HasMaxLength(30); builder.Property(x => x.BookingStatus).HasConversion<string>().HasMaxLength(30); builder.Property(x => x.Version).IsRowVersion();
            builder.HasOne<Organization>().WithMany().HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(x => x.CustomerProfile).WithOne(x => x.User).HasForeignKey<CustomerProfile>(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            builder.HasOne(x => x.EmployeeProfile).WithOne(x => x.User).HasForeignKey<EmployeeProfile>(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<CustomerProfile>(builder => { builder.ToTable("customer_profiles"); builder.HasKey(x => x.Id); builder.HasIndex(x => x.UserId).IsUnique(); builder.Property(x => x.Version).IsRowVersion(); });
        modelBuilder.Entity<EmployeeProfile>(builder => { builder.ToTable("employee_profiles"); builder.HasKey(x => x.Id); builder.HasIndex(x => x.UserId).IsUnique(); builder.Property(x => x.JobTitle).HasMaxLength(120); builder.Property(x => x.Version).IsRowVersion(); });
        modelBuilder.Entity<RefreshToken>(builder => { builder.ToTable("refresh_tokens"); builder.HasKey(x => x.Id); builder.HasIndex(x => x.TokenHash).IsUnique(); builder.HasIndex(x => new { x.UserId, x.ExpiresAt }); builder.Property(x => x.TokenHash).HasMaxLength(64); builder.Property(x => x.ReplacedByTokenHash).HasMaxLength(64); builder.Property(x => x.CreatedByIp).HasMaxLength(64); builder.Property(x => x.Version).IsRowVersion(); builder.HasOne(x => x.User).WithMany(x => x.RefreshTokens).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade); });
        modelBuilder.Entity<OtpCode>(builder => { builder.ToTable("otp_codes"); builder.HasKey(x => x.Id); builder.HasIndex(x => new { x.UserId, x.Purpose, x.ExpiresAt }); builder.Property(x => x.Purpose).HasConversion<string>().HasMaxLength(30); builder.Property(x => x.CodeHash).HasMaxLength(200); builder.Property(x => x.Version).IsRowVersion(); builder.HasOne(x => x.User).WithMany(x => x.OtpCodes).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade); });
        modelBuilder.Entity<Notification>(builder => { builder.ToTable("notifications"); builder.HasKey(x => x.Id); builder.HasIndex(x => new { x.UserId, x.IsRead, x.CreatedAt }); builder.Property(x => x.Channel).HasConversion<string>().HasMaxLength(20); builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20); builder.Property(x => x.Type).HasMaxLength(100); builder.Property(x => x.Subject).HasMaxLength(300); builder.Property(x => x.DeepLink).HasMaxLength(500); builder.Property(x => x.Version).IsRowVersion(); builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade); });
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
