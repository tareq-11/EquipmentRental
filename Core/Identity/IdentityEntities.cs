using Core.Common;

namespace Core.Identity;

public enum UserRole { Customer, OperationsEmployee, Admin }
public enum AccountStatus { Active, Suspended, Disabled }
public enum BookingStatus { Eligible, Suspended }
public enum OtpPurpose { EmailVerification, PasswordReset }
public enum NotificationChannel { Email, InApp }
public enum NotificationStatus { Pending, Delivered, Failed }

/// <summary>Future-ready owning organization for the single-vendor MVP.</summary>
public sealed class Organization : Entity
{
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; init; }
}

/// <summary>Authentication aggregate and role boundary.</summary>
public sealed class User : Entity
{
    public Guid OrganizationId { get; init; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    /// <summary>Invalidates every issued access token when account security changes.</summary>
    public string SecurityStamp { get; set; } = Guid.NewGuid().ToString("N");
    public UserRole Role { get; set; } = UserRole.Customer;
    public AccountStatus AccountStatus { get; set; } = AccountStatus.Active;
    public BookingStatus BookingStatus { get; set; } = BookingStatus.Eligible;
    public bool IsEmailVerified { get; set; }
    public bool IsPhoneConfirmed { get; set; }
    public bool TermsAccepted { get; init; }
    public DateTimeOffset? TermsAcceptedAt { get; init; }
    public int FailedLoginCount { get; set; }
    public DateTimeOffset? LockoutEnd { get; set; }
    public DateTimeOffset CreatedAt { get; init; }
    public CustomerProfile? CustomerProfile { get; set; }
    public EmployeeProfile? EmployeeProfile { get; set; }
    public ICollection<RefreshToken> RefreshTokens { get; } = new List<RefreshToken>();
    public ICollection<OtpCode> OtpCodes { get; } = new List<OtpCode>();
}

public sealed class CustomerProfile : Entity
{
    public Guid UserId { get; init; }
    public int NoShowCount { get; set; }
    public User? User { get; set; }
}

public sealed class EmployeeProfile : Entity
{
    public Guid UserId { get; init; }
    public string? JobTitle { get; set; }
    public bool IsActive { get; set; } = true;
    public User? User { get; set; }
}

public sealed class RefreshToken : Entity
{
    public Guid UserId { get; init; }
    public string TokenHash { get; init; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? RevokedAt { get; set; }
    public string? ReplacedByTokenHash { get; set; }
    public string? CreatedByIp { get; init; }
    public User? User { get; set; }
}

public sealed class OtpCode : Entity
{
    public Guid UserId { get; init; }
    public OtpPurpose Purpose { get; init; }
    public string CodeHash { get; init; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? ConsumedAt { get; set; }
    public int AttemptCount { get; set; }
    public User? User { get; set; }
}

/// <summary>Persisted lifecycle notification. Delivery content is held only in protected outbox payloads.</summary>
public sealed class Notification : Entity
{
    public Guid UserId { get; init; }
    public NotificationChannel Channel { get; init; }
    public string Type { get; init; } = string.Empty;
    public string Subject { get; init; } = string.Empty;
    public string? DeepLink { get; init; }
    public NotificationStatus Status { get; set; } = NotificationStatus.Pending;
    public bool IsRead { get; set; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? DeliveredAt { get; set; }
    public User? User { get; set; }
}
