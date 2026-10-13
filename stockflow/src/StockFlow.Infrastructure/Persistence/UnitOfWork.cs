using Microsoft.EntityFrameworkCore;
using StockFlow.Application.Abstractions;
using StockFlow.Application.Exceptions;

namespace StockFlow.Infrastructure.Persistence;

internal sealed class UnitOfWork(AppDbContext db) : IUnitOfWork
{
    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new ConcurrencyConflictException("The data was changed by someone else. Reload it and try again.", ex);
        }
    }
}