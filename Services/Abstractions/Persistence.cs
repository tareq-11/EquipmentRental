using Core.Common;

namespace Services.Abstractions;

/// <summary>Repository contract for aggregate roots; queries must project manually in handlers.</summary>
public interface IRepository<T> where T : Entity
{
    /// <summary>Gets an aggregate by identifier.</summary>
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    /// <summary>Adds an aggregate.</summary>
    Task AddAsync(T entity, CancellationToken cancellationToken);
}

/// <summary>Owns application transaction completion.</summary>
public interface IUnitOfWork
{
    /// <summary>Persists aggregate changes, domain events, and outbox messages atomically.</summary>
    Task<Result<int>> SaveChangesAsync(CancellationToken cancellationToken);
}

/// <summary>Atomically persists and replays idempotent command outcomes.</summary>
public interface IIdempotencyCoordinator
{
    /// <summary>Runs a command while holding its durable actor/key lock.</summary>
    Task<IdempotencyOutcome<TResponse>> ExecuteAsync<TResponse>(string key, string requestName, string actorScope, string fingerprint, Func<Task<TResponse>> execute, Func<TResponse, bool> isSuccess, CancellationToken cancellationToken);
}

/// <summary>Describes whether an idempotency execution ran, replayed, or conflicted.</summary>
public sealed record IdempotencyOutcome<TResponse>(TResponse? Response, Error? Error, bool IsReplay);
