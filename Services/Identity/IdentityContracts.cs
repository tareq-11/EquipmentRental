using Core.Common;
using Core.Identity;

namespace Services.Identity;

public sealed record RegisterRequest(string FullName, string Email, string PhoneNumber, string Password, string PasswordConfirmation, bool TermsAccepted);
public sealed record LoginRequest(string Email, string Password);
public sealed record VerifyOtpRequest(string Email, string Code, OtpPurpose Purpose);
public sealed record ResetPasswordRequest(string Email, string Code, string NewPassword, string NewPasswordConfirmation);
public sealed record TokenResponse(string AccessToken, DateTimeOffset ExpiresAt, UserRole Role, bool IsEmailVerified)
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string RefreshToken { get; init; } = string.Empty;
}
public sealed record ProfileResponse(Guid Id, string FullName, string Email, string PhoneNumber, UserRole Role, AccountStatus AccountStatus, BookingStatus BookingStatus, bool IsEmailVerified, bool IsPhoneConfirmed);
public sealed record AdminUserRequest(string? FullName, UserRole? Role, AccountStatus? AccountStatus, BookingStatus? BookingStatus, string? Reason);
public sealed record PagedResponse<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);

/// <summary>Identity use cases, deliberately exposing DTOs rather than persistence entities.</summary>
public interface IIdentityService
{
    Task<Result> RegisterAsync(RegisterRequest request, string? ipAddress, CancellationToken cancellationToken);
    Task<Result> VerifyOtpAsync(VerifyOtpRequest request, CancellationToken cancellationToken);
    Task<Result> ResendOtpAsync(string email, OtpPurpose purpose, string? ipAddress, CancellationToken cancellationToken);
    Task<Result<TokenResponse>> LoginAsync(LoginRequest request, string? ipAddress, CancellationToken cancellationToken);
    Task<Result<TokenResponse>> RefreshAsync(string refreshToken, string? ipAddress, CancellationToken cancellationToken);
    Task<Result> LogoutAsync(string refreshToken, CancellationToken cancellationToken);
    Task<Result> RequestPasswordResetAsync(string email, string? ipAddress, CancellationToken cancellationToken);
    Task<Result> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken);
    Task<Result<ProfileResponse>> GetProfileAsync(Guid userId, CancellationToken cancellationToken);
    Task<Result> UpdateProfileAsync(Guid userId, string fullName, string phoneNumber, CancellationToken cancellationToken);
    Task<Result<PagedResponse<ProfileResponse>>> ListUsersAsync(int page, int pageSize, CancellationToken cancellationToken);
    Task<Result> AdminUpdateUserAsync(Guid actorId, Guid userId, AdminUserRequest request, CancellationToken cancellationToken);
    Task<Result> ConfirmPhoneAsync(Guid actorId, Guid userId, CancellationToken cancellationToken);
    Task<Result> EnsureBookingEligibleAsync(Guid userId, CancellationToken cancellationToken);
}
