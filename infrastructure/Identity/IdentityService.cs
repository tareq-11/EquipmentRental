using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Core.Common;
using Core.Identity;
using infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.DataProtection;
using Services.Identity;

namespace infrastructure.Identity;

public sealed class IdentityService(EquipmentRentalDbContext db, TimeProvider clock, IOptions<JwtOptions> jwtOptions, IOptions<IdentityOptions> identityOptions, IDataProtector protector) : IIdentityService
{
    private readonly JwtOptions _jwt = jwtOptions.Value;
    private readonly IdentityOptions _identity = identityOptions.Value;
    // The DI registration already scopes this protector to account-email-v1; applying it twice makes worker payloads undecryptable.
    private readonly IDataProtector _protector = protector;

    public async Task<Result> RegisterAsync(RegisterRequest request, string? ipAddress, CancellationToken ct)
    {
        var errors = ValidateRegistration(request);
        if (errors.Count > 0) return Result.Failure(Error.Validation(errors));
        var email = request.Email.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(x => x.Email == email, ct)) return Result.Failure(new("email_in_use", "An account already uses this email address.", ErrorType.Conflict));
        var now = clock.GetUtcNow();
        var organization = await db.Organizations.SingleOrDefaultAsync(x => x.IsActive, ct);
        if (organization is null) return Result.Failure(new("organization_unavailable", "Account registration is not available yet.", ErrorType.Conflict));
        var user = new User { OrganizationId = organization.Id, FullName = request.FullName.Trim(), Email = email, PhoneNumber = NormalizePhone(request.PhoneNumber), PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password), TermsAccepted = true, TermsAcceptedAt = now, CreatedAt = now, CustomerProfile = new CustomerProfile() };
        db.Users.Add(user);
        await QueueOtpAsync(user, OtpPurpose.EmailVerification, now, ct);
        Audit(null, "account.registered", user.Id, now, ipAddress);
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> VerifyOtpAsync(VerifyOtpRequest request, CancellationToken ct)
    {
        var user = await FindUserAsync(request.Email, ct);
        if (user is null) return Result.Failure(InvalidOtp());
        var otp = await LatestOtpAsync(user.Id, request.Purpose, ct);
        var now = clock.GetUtcNow();
        if (otp is null || otp.ExpiresAt <= now || otp.AttemptCount >= _identity.MaxOtpAttempts || !BCrypt.Net.BCrypt.Verify(request.Code, otp.CodeHash))
        {
            if (otp is not null) otp.AttemptCount++;
            await db.SaveChangesAsync(ct);
            return Result.Failure(InvalidOtp());
        }
        otp.ConsumedAt = now;
        if (request.Purpose == OtpPurpose.EmailVerification) user.IsEmailVerified = true;
        Audit(user.Id, request.Purpose == OtpPurpose.EmailVerification ? "account.email_verified" : "account.reset_verified", user.Id, now, null);
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> ResendOtpAsync(string email, OtpPurpose purpose, string? ip, CancellationToken ct)
    {
        var user = await FindUserAsync(email, ct);
        if (user is null) return Result.Success(); // Avoid account enumeration.
        var now = clock.GetUtcNow(); var latest = await LatestOtpAsync(user.Id, purpose, ct);
        if (latest is not null && latest.CreatedAt.AddMinutes(_identity.OtpResendMinutes) > now) return Result.Failure(new("otp_rate_limited", "Wait before requesting another code.", ErrorType.RateLimited));
        await QueueOtpAsync(user, purpose, now, ct); Audit(user.Id, "account.otp_resent", user.Id, now, ip); await db.SaveChangesAsync(ct); return Result.Success();
    }

    public async Task<Result<TokenResponse>> LoginAsync(LoginRequest request, string? ip, CancellationToken ct)
    {
        var user = await FindUserAsync(request.Email, ct); var now = clock.GetUtcNow();
        if (user is null || user.AccountStatus == AccountStatus.Disabled || user.LockoutEnd > now || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            if (user is not null) { user.FailedLoginCount++; if (user.FailedLoginCount >= _identity.MaxFailedLogins) user.LockoutEnd = now.AddMinutes(_identity.LockoutMinutes); await db.SaveChangesAsync(ct); }
            return Result<TokenResponse>.Failure(new("invalid_credentials", "Email or password is incorrect.", ErrorType.Unauthorized));
        }
        user.FailedLoginCount = 0; user.LockoutEnd = null; var response = await IssueTokensAsync(user, ip, ct); Audit(user.Id, "account.login", user.Id, now, ip); await db.SaveChangesAsync(ct); return Result<TokenResponse>.Success(response);
    }

    public async Task<Result<TokenResponse>> RefreshAsync(string rawToken, string? ip, CancellationToken ct)
    {
        var hash = Hash(rawToken); var token = await db.RefreshTokens.Include(x => x.User).SingleOrDefaultAsync(x => x.TokenHash == hash, ct); var now = clock.GetUtcNow();
        if (token is null || token.RevokedAt is not null || token.ExpiresAt <= now || token.User is null || token.User.AccountStatus == AccountStatus.Disabled) return Result<TokenResponse>.Failure(new("invalid_refresh_token", "The session has expired. Sign in again.", ErrorType.Unauthorized));
        token.RevokedAt = now; var response = await IssueTokensAsync(token.User, ip, ct); token.ReplacedByTokenHash = Hash(response.RefreshToken); await db.SaveChangesAsync(ct); return Result<TokenResponse>.Success(response);
    }

    public async Task<Result> LogoutAsync(string rawToken, CancellationToken ct) { var token = await db.RefreshTokens.SingleOrDefaultAsync(x => x.TokenHash == Hash(rawToken), ct); if (token is not null) { var now = clock.GetUtcNow(); token.RevokedAt = now; Audit(token.UserId, "account.logout", token.UserId, now, null); await db.SaveChangesAsync(ct); } return Result.Success(); }
    public async Task<Result> RequestPasswordResetAsync(string email, string? ip, CancellationToken ct) { var user = await FindUserAsync(email, ct); if (user is null) return Result.Success(); var now = clock.GetUtcNow(); var latest = await LatestOtpAsync(user.Id, OtpPurpose.PasswordReset, ct); if (latest is not null && latest.CreatedAt.AddMinutes(_identity.OtpResendMinutes) > now) return Result.Failure(new("otp_rate_limited", "Wait before requesting another code.", ErrorType.RateLimited)); await QueueOtpAsync(user, OtpPurpose.PasswordReset, now, ct); Audit(user.Id, "account.password_reset_requested", user.Id, now, ip); await db.SaveChangesAsync(ct); return Result.Success(); }
    public async Task<Result> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct)
    {
        var verification = await VerifyOtpAsync(new(request.Email, request.Code, OtpPurpose.PasswordReset), ct); if (!verification.IsSuccess) return verification;
        var passwordError = PasswordPolicy.Validate(request.NewPassword);
        if (passwordError is not null || request.NewPassword != request.NewPasswordConfirmation)
            return Result.Failure(Error.Validation(new Dictionary<string, string[]> { ["newPassword"] = passwordError is null ? [] : [passwordError], ["newPasswordConfirmation"] = request.NewPassword == request.NewPasswordConfirmation ? [] : ["Passwords do not match."] }));
        var user = await FindUserAsync(request.Email, ct); if (user is null) return Result.Failure(InvalidOtp()); user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword); RotateSecurityStamp(user); await RevokeTokensAsync(user.Id, ct); Audit(user.Id, "account.password_reset", user.Id, clock.GetUtcNow(), null); await db.SaveChangesAsync(ct); return Result.Success();
    }
    public async Task<Result<ProfileResponse>> GetProfileAsync(Guid id, CancellationToken ct) { var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct); return user is null ? Result<ProfileResponse>.Failure(new("not_found", "Account not found.", ErrorType.NotFound)) : Result<ProfileResponse>.Success(Map(user)); }
    public async Task<Result> UpdateProfileAsync(Guid id, string fullName, string phone, CancellationToken ct) { var user = await db.Users.SingleOrDefaultAsync(x => x.Id == id, ct); if (user is null) return Result.Failure(new("not_found", "Account not found.", ErrorType.NotFound)); if (string.IsNullOrWhiteSpace(fullName) || !IsJordanPhone(phone)) return Result.Failure(Error.Validation(new Dictionary<string, string[]> { ["profile"] = ["Provide a name and Jordanian mobile number."] })); var oldValues = $"name={user.FullName};phone={user.PhoneNumber}"; user.FullName = fullName.Trim(); user.PhoneNumber = NormalizePhone(phone); Audit(user.Id, "account.profile_updated", user.Id, clock.GetUtcNow(), null, null, oldValues, $"name={user.FullName};phone={user.PhoneNumber}"); await db.SaveChangesAsync(ct); return Result.Success(); }
    public async Task<Result<PagedResponse<ProfileResponse>>> ListUsersAsync(int page, int pageSize, CancellationToken ct)
    {
        if (page < 1 || pageSize is < 1 or > 100) return Result<PagedResponse<ProfileResponse>>.Failure(Error.Validation(new Dictionary<string, string[]> { ["page"] = ["Page must be at least 1."], ["pageSize"] = ["Page size must be between 1 and 100."] }));
        var query = db.Users.AsNoTracking().OrderBy(x => x.Email);
        var total = await query.CountAsync(ct);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).Select(x => new ProfileResponse(x.Id, x.FullName, x.Email, x.PhoneNumber, x.Role, x.AccountStatus, x.BookingStatus, x.IsEmailVerified, x.IsPhoneConfirmed)).ToListAsync(ct);
        return Result<PagedResponse<ProfileResponse>>.Success(new(items, page, pageSize, total));
    }
    public async Task<Result> AdminUpdateUserAsync(Guid actorId, Guid id, AdminUserRequest request, CancellationToken ct)
    {
        var user = await db.Users.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (user is null) return Result.Failure(new("not_found", "Account not found.", ErrorType.NotFound));
        if ((request.Role.HasValue || request.AccountStatus.HasValue || request.BookingStatus.HasValue) && string.IsNullOrWhiteSpace(request.Reason)) return Result.Failure(Error.Validation(new Dictionary<string, string[]> { ["reason"] = ["A reason is required for this security change."] }));
        var oldValues = $"role={user.Role};account={user.AccountStatus};booking={user.BookingStatus}";
        if (request.FullName is not null) user.FullName = request.FullName.Trim();
        var changesSecurity = (request.Role.HasValue && request.Role.Value != user.Role) || (request.AccountStatus.HasValue && request.AccountStatus.Value != user.AccountStatus);
        if (request.Role.HasValue && request.Role.Value != user.Role)
        {
            user.Role = request.Role.Value;
            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            if (user.Role == UserRole.Customer)
            {
                await db.EmployeeProfiles.Where(x => x.UserId == user.Id).ExecuteDeleteAsync(ct);
                if (!await db.CustomerProfiles.AnyAsync(x => x.UserId == user.Id, ct))
                    db.CustomerProfiles.Add(new CustomerProfile { UserId = user.Id });
            }
            else
            {
                await db.CustomerProfiles.Where(x => x.UserId == user.Id).ExecuteDeleteAsync(ct);
                if (!await db.EmployeeProfiles.AnyAsync(x => x.UserId == user.Id, ct))
                    db.EmployeeProfiles.Add(new EmployeeProfile { UserId = user.Id, JobTitle = user.Role == UserRole.Admin ? "Administrator" : "Operations" });
            }
            if (request.AccountStatus.HasValue) user.AccountStatus = request.AccountStatus.Value;
            if (request.BookingStatus.HasValue) user.BookingStatus = request.BookingStatus.Value;
            if (changesSecurity) { RotateSecurityStamp(user); await RevokeTokensAsync(user.Id, ct); }
            Audit(actorId, "admin.account_updated", user.Id, clock.GetUtcNow(), null, request.Reason, oldValues, $"role={user.Role};account={user.AccountStatus};booking={user.BookingStatus}");
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return Result.Success();
        }
        if (request.AccountStatus.HasValue) user.AccountStatus = request.AccountStatus.Value;
        if (request.BookingStatus.HasValue) user.BookingStatus = request.BookingStatus.Value;
        if (changesSecurity) { RotateSecurityStamp(user); await RevokeTokensAsync(user.Id, ct); }
        Audit(actorId, "admin.account_updated", user.Id, clock.GetUtcNow(), null, request.Reason, oldValues, $"role={user.Role};account={user.AccountStatus};booking={user.BookingStatus}");
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
    public async Task<Result> ConfirmPhoneAsync(Guid actorId, Guid id, CancellationToken ct)
    {
        var user = await db.Users.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (user is null) return Result.Failure(new("not_found", "Account not found.", ErrorType.NotFound));
        if (user.Role != UserRole.Customer) return Result.Failure(new("phone_confirmation_not_applicable", "Only customer phone numbers can be confirmed.", ErrorType.Validation));
        user.IsPhoneConfirmed = true;
        Audit(actorId, "operations.phone_confirmed", id, clock.GetUtcNow(), null);
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> EnsureBookingEligibleAsync(Guid userId, CancellationToken ct)
    {
        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == userId, ct);
        if (user is null) return Result.Failure(new("not_found", "Account not found.", ErrorType.NotFound));
        if (user.Role != UserRole.Customer || !user.IsEmailVerified || user.BookingStatus != BookingStatus.Eligible)
            return Result.Failure(new("booking_not_eligible", "A verified customer account with active booking access is required.", ErrorType.Forbidden));
        return Result.Success();
    }

    private async Task QueueOtpAsync(User user, OtpPurpose purpose, DateTimeOffset now, CancellationToken ct) { var code = RandomNumberGenerator.GetInt32(100000, 1000000).ToString(); db.OtpCodes.Add(new OtpCode { UserId = user.Id, Purpose = purpose, CodeHash = BCrypt.Net.BCrypt.HashPassword(code), CreatedAt = now, ExpiresAt = now.AddMinutes(_identity.OtpMinutes) }); var subject = purpose == OtpPurpose.EmailVerification ? "Verify your Stagehand email" : "Reset your Stagehand password"; var notification = new Notification { UserId = user.Id, Channel = NotificationChannel.Email, Type = purpose.ToString(), Subject = subject, CreatedAt = now }; db.Notifications.Add(notification); db.OutboxMessages.Add(new OutboxMessage { EventId = notification.Id, Type = "account.email", Payload = _protector.Protect($"{user.Email}\n{subject}\nYour security code is {code}. It expires in {_identity.OtpMinutes} minutes."), OccurredAt = now }); await Task.CompletedTask; }
    private async Task<TokenResponse> IssueTokensAsync(User user, string? ip, CancellationToken ct) { var now = clock.GetUtcNow(); var expires = now.AddMinutes(_jwt.AccessTokenMinutes); var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.SigningKey)); var jwt = new JwtSecurityToken(_jwt.Issuer, _jwt.Audience, [new(ClaimTypes.NameIdentifier, user.Id.ToString()), new(ClaimTypes.Email, user.Email), new(ClaimTypes.Role, user.Role.ToString()), new("sst", user.SecurityStamp)], now.UtcDateTime, expires.UtcDateTime, new SigningCredentials(key, SecurityAlgorithms.HmacSha256)); var raw = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)); db.RefreshTokens.Add(new RefreshToken { UserId = user.Id, TokenHash = Hash(raw), CreatedAt = now, ExpiresAt = now.AddDays(_jwt.RefreshTokenDays), CreatedByIp = ip }); await Task.CompletedTask; return new(new JwtSecurityTokenHandler().WriteToken(jwt), expires, user.Role, user.IsEmailVerified) { RefreshToken = raw }; }
    private async Task<User?> FindUserAsync(string email, CancellationToken ct) => await db.Users.Include(x => x.EmployeeProfile).SingleOrDefaultAsync(x => x.Email == email.Trim().ToLowerInvariant(), ct);
    private async Task<OtpCode?> LatestOtpAsync(Guid id, OtpPurpose purpose, CancellationToken ct) => await db.OtpCodes.Where(x => x.UserId == id && x.Purpose == purpose && x.ConsumedAt == null).OrderByDescending(x => x.CreatedAt).FirstOrDefaultAsync(ct);
    private async Task RevokeTokensAsync(Guid userId, CancellationToken ct) { var now = clock.GetUtcNow(); foreach (var token in await db.RefreshTokens.Where(x => x.UserId == userId && x.RevokedAt == null).ToListAsync(ct)) token.RevokedAt = now; }
    private void Audit(Guid? actor, string action, Guid target, DateTimeOffset at, string? ip, string? reason = null, string? oldValues = null, string? newValues = null) => db.AuditLogs.Add(new() { ActingUserId = actor, ActorType = actor.HasValue ? "user" : "anonymous", Action = action, TargetType = "User", TargetId = target.ToString(), OccurredAt = at, IpAddress = ip, Reason = reason, OldValues = oldValues, NewValues = newValues });
    private static ProfileResponse Map(User x) => new(x.Id, x.FullName, x.Email, x.PhoneNumber, x.Role, x.AccountStatus, x.BookingStatus, x.IsEmailVerified, x.IsPhoneConfirmed);
    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    private static Error InvalidOtp() => new("invalid_code", "The code is invalid or has expired.", ErrorType.Unauthorized);
    private static string NormalizePhone(string phone) => phone.Trim().Replace(" ", "").Replace("-", "").Replace("(0)", "");
    private static bool IsJordanPhone(string phone) => System.Text.RegularExpressions.Regex.IsMatch(NormalizePhone(phone), "^(?:\\+962|00962|0)7[789]\\d{7}$");
    private static void RotateSecurityStamp(User user) => user.SecurityStamp = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
    private static Dictionary<string, string[]> ValidateRegistration(RegisterRequest x) { var errors = new Dictionary<string, string[]>(); if (string.IsNullOrWhiteSpace(x.FullName)) errors["fullName"] = ["Enter your name."]; if (!System.Net.Mail.MailAddress.TryCreate(x.Email, out _)) errors["email"] = ["Enter a valid email."]; if (!IsJordanPhone(x.PhoneNumber)) errors["phoneNumber"] = ["Enter a Jordanian mobile number."]; var passwordError = PasswordPolicy.Validate(x.Password); if (passwordError is not null) errors["password"] = [passwordError]; if (x.Password != x.PasswordConfirmation) errors["passwordConfirmation"] = ["Passwords do not match."]; if (!x.TermsAccepted) errors["termsAccepted"] = ["Accept the terms to create an account."]; return errors; }
}
