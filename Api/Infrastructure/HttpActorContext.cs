using Services.Abstractions;
using System.Security.Claims;

namespace Api.Infrastructure;

/// <summary>Establishes an explicit authenticated, system, or anonymous actor convention from HTTP context.</summary>
public sealed class HttpActorContext(IHttpContextAccessor accessor) : IActorContext
{
    private readonly ClaimsPrincipal? _user = accessor.HttpContext?.User;
    /// <inheritdoc />
    public Guid? UserId => Guid.TryParse(_user?.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
    /// <inheritdoc />
    public string ActorType => UserId.HasValue ? "user" : "anonymous";
    /// <inheritdoc />
    public string Scope => UserId?.ToString("D") ?? "anonymous";
}
