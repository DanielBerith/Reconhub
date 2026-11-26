namespace ReconHub.Application.Abstractions.Persistence;

public interface IUnitOfWork
{
    /// <summary>Persists all pending changes atomically.</summary>
    /// <exception cref="ConcurrencyConflictException">Another operation changed the same data first.</exception>
    /// <exception cref="UniqueConstraintViolationException">A uniqueness rule was violated, typically by a concurrent insert.</exception>
    Task SaveChangesAsync(CancellationToken cancellationToken);

    /// <summary>Discards all pending changes and loaded entities so an operation can be retried from scratch.</summary>
    void Reset();
}
