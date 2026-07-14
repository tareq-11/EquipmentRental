using System.Text.Json;
using Core.Common;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Services.Abstractions;

namespace infrastructure.Persistence;

/// <summary>PostgreSQL-backed idempotency coordinator that serializes one actor/key command transaction.</summary>
public sealed class IdempotencyCoordinator(EquipmentRentalDbContext dbContext, TimeProvider clock) : IIdempotencyCoordinator
{
    /// <inheritdoc />
    public async Task<IdempotencyOutcome<TResponse>> ExecuteAsync<TResponse>(string key, string requestName, string actorScope, string fingerprint, Func<Task<TResponse>> execute, Func<TResponse, bool> isSuccess, CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            // PostgreSQL releases this advisory lock with the transaction, including on any rollback.
            await dbContext.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({$"{actorScope}:{requestName}:{key}"}, 0))", cancellationToken);
            var existing = await dbContext.IdempotentRequests.AsNoTracking().SingleOrDefaultAsync(x => x.Key == key && x.RequestName == requestName && x.ActorScope == actorScope, cancellationToken);
            if (existing is not null)
            {
                await transaction.RollbackAsync(cancellationToken);
                if (!string.Equals(existing.RequestFingerprint, fingerprint, StringComparison.Ordinal)) return new(default!, new Error("idempotency_key_reused", "This idempotency key was already used with a different request.", ErrorType.Conflict), false);
                return new(JsonSerializer.Deserialize<TResponse>(existing.ResponseJson), null, true);
            }

            var response = await execute();
            if (!isSuccess(response))
            {
                await transaction.RollbackAsync(cancellationToken);
                return new(response, null, false);
            }

            dbContext.IdempotentRequests.Add(new IdempotentRequest { Key = key, RequestName = requestName, ActorScope = actorScope, RequestFingerprint = fingerprint, ResponseJson = JsonSerializer.Serialize(response), CreatedAt = clock.GetUtcNow() });
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new(response, null, false);
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            await transaction.RollbackAsync(cancellationToken);
            dbContext.ChangeTracker.Clear();
            var existing = await dbContext.IdempotentRequests.AsNoTracking().SingleOrDefaultAsync(x => x.Key == key && x.RequestName == requestName && x.ActorScope == actorScope, cancellationToken);
            if (existing is not null && string.Equals(existing.RequestFingerprint, fingerprint, StringComparison.Ordinal)) return new(JsonSerializer.Deserialize<TResponse>(existing.ResponseJson), null, true);
            return new(default!, new Error("idempotency_key_reused", "This idempotency key was already used with a different request.", ErrorType.Conflict), false);
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(cancellationToken);
            dbContext.ChangeTracker.Clear();
            return new(default!, new Error("concurrency_conflict", "This record changed before your update. Refresh and try again.", ErrorType.Conflict), false);
        }
    }
}
