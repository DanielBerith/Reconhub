using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ReconHub.Application.Abstractions.Persistence;

namespace ReconHub.Infrastructure.Persistence;

internal sealed class EfUnitOfWork(ReconHubDbContext db) : IUnitOfWork
{
    // SQL Server: 2601 = duplicate key in unique index, 2627 = unique/primary key constraint violation.
    private const int DuplicateKeyInIndex = 2601;
    private const int UniqueConstraintViolation = 2627;

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new ConcurrencyConflictException("The data was modified by another operation.", ex);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: DuplicateKeyInIndex or UniqueConstraintViolation })
        {
            throw new UniqueConstraintViolationException("A record with the same unique key already exists.", ex);
        }
    }

    public void Reset() => db.ChangeTracker.Clear();
}
