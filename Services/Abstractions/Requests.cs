using Core.Common;
using MediatR;

namespace Services.Abstractions;

/// <summary>Marker for a command returning a result.</summary>
public interface ICommand<TResponse> : IRequest<Result<TResponse>>;
/// <summary>Marker for a read-only query returning a result.</summary>
public interface IQuery<TResponse> : IRequest<Result<TResponse>>;
/// <summary>Marks a command that requires idempotency persistence.</summary>
public interface IIdempotentCommand { string IdempotencyKey { get; } }
/// <summary>Lets the idempotency pipeline mark a replayed successful payload.</summary>
public interface IIdempotentReplayResponse { object AsIdempotentReplay(); }
/// <summary>Provides the current authenticated, system, or anonymous actor for application actions.</summary>
public interface IActorContext
{
    /// <summary>Gets the authenticated actor identifier when one is present.</summary>
    Guid? UserId { get; }
    /// <summary>Gets a durable scope for idempotency keys and audit records.</summary>
    string Scope { get; }
    /// <summary>Gets the actor convention used when no authenticated user is present.</summary>
    string ActorType { get; }
}
/// <summary>Marks a query eligible for non-authoritative caching.</summary>
public interface ICacheableQuery { string CacheKey { get; } TimeSpan Duration { get; } }
