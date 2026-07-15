using System.ComponentModel.DataAnnotations;

namespace infrastructure.Identity;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";
    [Required, MinLength(32)] public string SigningKey { get; init; } = string.Empty;
    public string Issuer { get; init; } = "EquipmentRental";
    public string Audience { get; init; } = "EquipmentRental.Web";
    [Range(5, 60)] public int AccessTokenMinutes { get; init; } = 15;
    [Range(1, 90)] public int RefreshTokenDays { get; init; } = 14;
}

public sealed class IdentityOptions
{
    public const string SectionName = "Identity";
    [Range(1, 20)] public int OtpMinutes { get; init; } = 10;
    [Range(1, 20)] public int OtpResendMinutes { get; init; } = 1;
    [Range(1, 20)] public int MaxOtpAttempts { get; init; } = 5;
    [Range(1, 20)] public int MaxFailedLogins { get; init; } = 5;
    [Range(1, 1440)] public int LockoutMinutes { get; init; } = 15;
}

public sealed class MailOptions
{
    public const string SectionName = "Mail";
    [Required] public string Mode { get; init; } = "DevelopmentMailbox";
    public string FromAddress { get; init; } = "no-reply@localhost";
    public string FromName { get; init; } = "Stagehand";
    public string? Host { get; init; }
    [Range(1, 65535)] public int Port { get; init; } = 587;
    [Required] public string TlsMode { get; init; } = "StartTls";
    public string? UserName { get; init; }
    public string? Password { get; init; }
    public string? DevelopmentMailboxPath { get; init; }
}
