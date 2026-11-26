using Microsoft.EntityFrameworkCore;
using ReconHub.Application.Abstractions.Persistence;
using ReconHub.Domain.Auditing;
using ReconHub.Domain.Events;
using ReconHub.Domain.Flows;
using ReconHub.Domain.Transactions;

namespace ReconHub.Infrastructure.Persistence.Repositories;

// Every query below is restricted to the current tenant by the global query filter.

internal sealed class IntegrationEventRepository(ReconHubDbContext db) : IIntegrationEventRepository
{
    public Task<IntegrationEvent?> FindByIdempotencyKeyAsync(Guid sourceSystemId, string idempotencyKey, CancellationToken cancellationToken) =>
        db.IntegrationEvents.FirstOrDefaultAsync(
            e => e.SourceSystemId == sourceSystemId && e.IdempotencyKey == idempotencyKey,
            cancellationToken);

    public void Add(IntegrationEvent integrationEvent) => db.IntegrationEvents.Add(integrationEvent);
}

internal sealed class FlowDefinitionRepository(ReconHubDbContext db) : IFlowDefinitionRepository
{
    public async Task<IReadOnlyList<FlowDefinition>> ListActiveForEventAsync(
        Guid sourceSystemId,
        string eventType,
        CancellationToken cancellationToken) =>
        await db.FlowDefinitions
            .Where(f => f.IsActive
                && ((f.TriggerSourceSystemId == sourceSystemId && f.TriggerEventType == eventType)
                    || (f.FollowUpSourceSystemId == sourceSystemId && f.FollowUpEventType == eventType)))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<FlowDefinition>> ListByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken) =>
        await db.FlowDefinitions.Where(f => ids.Contains(f.Id)).ToListAsync(cancellationToken);
}

internal sealed class TransactionRepository(ReconHubDbContext db) : ITransactionRepository
{
    public Task<Transaction?> FindByBusinessKeyAsync(Guid flowDefinitionId, string businessKey, CancellationToken cancellationToken) =>
        db.Transactions
            .Include(t => t.Events)
            .FirstOrDefaultAsync(t => t.FlowDefinitionId == flowDefinitionId && t.BusinessKey == businessKey, cancellationToken);

    public Task<Transaction?> FindByExceptionIdAsync(Guid exceptionId, CancellationToken cancellationToken) =>
        db.Transactions
            .Include(t => t.Exceptions)
            .FirstOrDefaultAsync(t => t.Exceptions.Any(e => e.Id == exceptionId), cancellationToken);

    public async Task<IReadOnlyList<Transaction>> ListDueForEvaluationAsync(
        DateTimeOffset now,
        int batchSize,
        CancellationToken cancellationToken) =>
        await db.Transactions
            .Where(t => t.EvaluationDueAt != null && t.EvaluationDueAt <= now)
            .OrderBy(t => t.EvaluationDueAt)
            .ThenBy(t => t.Id)
            .Take(batchSize)
            .Include(t => t.Events)
            .Include(t => t.Exceptions)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Guid>> ListIdsForEventAsync(Guid integrationEventId, CancellationToken cancellationToken) =>
        await db.Transactions
            .Where(t => t.Events.Any(e => e.Id == integrationEventId))
            .Select(t => t.Id)
            .ToListAsync(cancellationToken);

    public void Add(Transaction transaction) => db.Transactions.Add(transaction);
}

internal sealed class AuditLog(ReconHubDbContext db) : IAuditLog
{
    public void Add(AuditEntry entry) => db.AuditEntries.Add(entry);
}
