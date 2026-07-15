using Api.Middleware;
using Core.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Services.Identity;
using System.Security.Claims;
using System.Security.Cryptography;

namespace Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IIdentityService identity) : ControllerBase
{
    [HttpPost("register")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Register(RegisterRequest request, CancellationToken ct) => Map(await identity.RegisterAsync(request, HttpContext.Connection.RemoteIpAddress?.ToString(), ct), "Registration created. Check your email for a verification code.", StatusCodes.Status201Created);
    [HttpPost("verify-email")]
    [EnableRateLimiting("otp")]
    public async Task<IActionResult> VerifyEmail(VerifyOtpRequest request, CancellationToken ct) => Map(await identity.VerifyOtpAsync(request with { Purpose = OtpPurpose.EmailVerification }, ct), "Email verified.");
    [HttpPost("resend-verification")]
    [EnableRateLimiting("otp")]
    public async Task<IActionResult> Resend(EmailRequest request, CancellationToken ct) => Map(await identity.ResendOtpAsync(request.Email, OtpPurpose.EmailVerification, HttpContext.Connection.RemoteIpAddress?.ToString(), ct), "If the account exists, a code is on its way.");
    [HttpPost("login")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken ct) => await StartSessionAsync(await identity.LoginAsync(request, HttpContext.Connection.RemoteIpAddress?.ToString(), ct), "Signed in.");
    [HttpGet("csrf")]
    public IActionResult Csrf()
    {
        var csrfToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        Response.Cookies.Append("er_csrf", csrfToken, CookieOptions(false, DateTimeOffset.UtcNow.AddDays(1)));
        Response.Headers.CacheControl = "no-store";
        return Ok(new Shared.ApiResponse<object>(StatusCodes.Status200OK, "CSRF token issued.", new { csrfToken }));
    }
    [HttpPost("refresh")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Refresh(CancellationToken ct)
    {
        if (!HasValidCsrfToken()) return CsrfDenied();
        return await StartSessionAsync(await identity.RefreshAsync(Request.Cookies["er_refresh"] ?? string.Empty, HttpContext.Connection.RemoteIpAddress?.ToString(), ct), "Session refreshed.");
    }
    [HttpPost("logout")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        if (!HasValidCsrfToken()) return CsrfDenied();
        var result = await identity.LogoutAsync(Request.Cookies["er_refresh"] ?? string.Empty, ct);
        DeleteSessionCookies();
        return Map(result, "Signed out.");
    }
    [HttpPost("forgot-password")]
    [EnableRateLimiting("otp")]
    public async Task<IActionResult> Forgot(EmailRequest request, CancellationToken ct) => Map(await identity.RequestPasswordResetAsync(request.Email, HttpContext.Connection.RemoteIpAddress?.ToString(), ct), "If the account exists, a reset code is on its way.");
    [HttpPost("reset-password")]
    [EnableRateLimiting("otp")]
    public async Task<IActionResult> Reset(ResetPasswordRequest request, CancellationToken ct) => Map(await identity.ResetPasswordAsync(request, ct), "Password reset. Sign in with the new password.");
    [Authorize]
    [HttpGet("profile")]
    public async Task<IActionResult> Profile(CancellationToken ct) => ApiResponseMapper.ToActionResult(this, await identity.GetProfileAsync(UserId(), ct), "Profile loaded.");
    [Authorize]
    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile(UpdateProfileRequest request, CancellationToken ct) => Map(await identity.UpdateProfileAsync(UserId(), request.FullName, request.PhoneNumber, ct), "Profile updated.");
    [Authorize(Roles = "Customer")]
    [HttpGet("booking-eligibility")]
    public async Task<IActionResult> BookingEligibility(CancellationToken ct) => Map(await identity.EnsureBookingEligibleAsync(UserId(), ct), "Booking access is active.");

    private Guid UserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private IActionResult Map(Core.Common.Result result, string message, int success = StatusCodes.Status200OK) => result.IsSuccess ? StatusCode(success, new Shared.ApiResponse<object?>(success, message, null)) : StatusCode(ApiResponseMapper.StatusCode(result.Error!), new Shared.ApiResponse<Shared.ApiErrorData>(ApiResponseMapper.StatusCode(result.Error!), result.Error!.Message, new Shared.ApiErrorData(result.Error!.Fields)));
    private async Task<IActionResult> StartSessionAsync(Core.Common.Result<TokenResponse> result, string message)
    {
        if (!result.IsSuccess) return ApiResponseMapper.ToActionResult(this, result, message);
        var csrfToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        Response.Cookies.Append("er_refresh", result.Value!.RefreshToken, CookieOptions(true, result.Value.ExpiresAt));
        Response.Cookies.Append("er_csrf", csrfToken, CookieOptions(false, result.Value.ExpiresAt));
        return Ok(new Shared.ApiResponse<object>(StatusCodes.Status200OK, message, new { result.Value.AccessToken, result.Value.ExpiresAt, result.Value.Role, result.Value.IsEmailVerified, csrfToken }));
    }
    private bool HasValidCsrfToken() => Request.Headers.TryGetValue("X-CSRF-TOKEN", out var header) && Request.Cookies.TryGetValue("er_csrf", out var cookie) && CryptographicOperations.FixedTimeEquals(System.Text.Encoding.UTF8.GetBytes(header.ToString()), System.Text.Encoding.UTF8.GetBytes(cookie));
    private IActionResult CsrfDenied() => StatusCode(StatusCodes.Status403Forbidden, new Shared.ApiResponse<Shared.ApiErrorData>(StatusCodes.Status403Forbidden, "A valid CSRF token is required.", new Shared.ApiErrorData(null)));
    private static CookieOptions CookieOptions(bool httpOnly, DateTimeOffset expires) => new() { HttpOnly = httpOnly, Secure = true, SameSite = SameSiteMode.Strict, Path = "/api/auth", Expires = expires };
    private void DeleteSessionCookies() { Response.Cookies.Delete("er_refresh", CookieOptions(true, DateTimeOffset.UnixEpoch)); Response.Cookies.Delete("er_csrf", CookieOptions(false, DateTimeOffset.UnixEpoch)); }
}
public sealed record EmailRequest(string Email);
public sealed record UpdateProfileRequest(string FullName, string PhoneNumber);
