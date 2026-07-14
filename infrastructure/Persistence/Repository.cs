using Core.Common;
using Microsoft.EntityFrameworkCore;
using Services.Abstractions;

namespace infrastructure.Persistence;

/// <summary>EF implementation used by application handlers for aggregate persistence.</summary>
public sealed class Repository<T>(EquipmentRentalDbContext dbContext) : IRepository<T> where T : Entity
{
    public Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken) => dbContext.Set<T>().SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
    public Task AddAsync(T entity, CancellationToken cancellationToken) => dbContext.Set<T>().AddAsync(entity, cancellationToken).AsTask();
}

/// <summary>Completes an application-owned unit of work.</summary>
public sealed class UnitOfWork(EquipmentRentalDbContext dbContext) : IUnitOfWork
{
    public async Task<Result<int>> SaveChangesAsync(CancellationToken cancellationToken)
    {
        try { return Result<int>.Success(await dbContext.SaveChangesAsync(cancellationToken)); }
        catch (DbUpdateConcurrencyException)
        {
            dbContext.ChangeTracker.Clear();
            return Result<int>.Failure(new Error("concurrency_conflict", "This record changed before your update. Refresh and try again.", ErrorType.Conflict));
        }
    }
}
