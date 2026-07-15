using Core.Identity;
using infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace infrastructure.Identity;

public sealed class BootstrapOptions
{
    public const string SectionName = "Bootstrap";
    public string? OrganizationName { get; init; }
    public string? AdminEmail { get; init; }
    public string? AdminPassword { get; init; }
    public string? OperationsEmail { get; init; }
    public string? OperationsPassword { get; init; }
}

/// <summary>Creates initial staff only when all explicit bootstrap secrets are supplied to an empty database.</summary>
public sealed class BootstrapWorker(IServiceScopeFactory scopes, IOptions<BootstrapOptions> options, TimeProvider clock, ILogger<BootstrapWorker> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken ct)
    {
        var value = options.Value;
        if (string.IsNullOrWhiteSpace(value.OrganizationName) && string.IsNullOrWhiteSpace(value.AdminEmail) && string.IsNullOrWhiteSpace(value.OperationsEmail)) return;
        if (new[] { value.OrganizationName, value.AdminEmail, value.AdminPassword, value.OperationsEmail, value.OperationsPassword }.Any(string.IsNullOrWhiteSpace)) { logger.LogCritical("Bootstrap configuration is incomplete; no accounts were created."); return; }
        if (!System.Net.Mail.MailAddress.TryCreate(value.AdminEmail, out _) || !System.Net.Mail.MailAddress.TryCreate(value.OperationsEmail, out _) || string.Equals(value.AdminEmail.Trim(), value.OperationsEmail.Trim(), StringComparison.OrdinalIgnoreCase) || PasswordPolicy.Validate(value.AdminPassword) is not null || PasswordPolicy.Validate(value.OperationsPassword) is not null)
        {
            logger.LogCritical("Bootstrap configuration is invalid; no accounts were created.");
            return;
        }
        await using var scope = scopes.CreateAsyncScope(); var db = scope.ServiceProvider.GetRequiredService<EquipmentRentalDbContext>();
        if (await db.Organizations.AnyAsync(ct) || await db.Users.AnyAsync(ct)) { logger.LogInformation("Bootstrap skipped because organization or users already exist."); return; }
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var now = clock.GetUtcNow(); var org = new Organization { Name = value.OrganizationName!.Trim(), CreatedAt = now }; db.Organizations.Add(org);
        db.Users.AddRange(new User { OrganizationId = org.Id, FullName = "Initial Administrator", Email = value.AdminEmail!.Trim().ToLowerInvariant(), PhoneNumber = "0790000000", PasswordHash = BCrypt.Net.BCrypt.HashPassword(value.AdminPassword!), Role = UserRole.Admin, IsEmailVerified = true, IsPhoneConfirmed = true, TermsAccepted = true, TermsAcceptedAt = now, CreatedAt = now, EmployeeProfile = new EmployeeProfile { JobTitle = "Administrator" } }, new User { OrganizationId = org.Id, FullName = "Initial Operations Employee", Email = value.OperationsEmail!.Trim().ToLowerInvariant(), PhoneNumber = "0790000001", PasswordHash = BCrypt.Net.BCrypt.HashPassword(value.OperationsPassword!), Role = UserRole.OperationsEmployee, IsEmailVerified = true, IsPhoneConfirmed = true, TermsAccepted = true, TermsAcceptedAt = now, CreatedAt = now, EmployeeProfile = new EmployeeProfile { JobTitle = "Operations" } });
        await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct); logger.LogWarning("Initial organization and staff accounts were bootstrapped. Remove bootstrap secrets now.");
    }
    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
