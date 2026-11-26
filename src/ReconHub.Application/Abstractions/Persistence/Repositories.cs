using ReconHub.Application.Exceptions;
using ReconHub.Application.Common;
using ReconHub.Domain.Auditing;
using ReconHub.Domain.Events;
using ReconHub.Domain.Flows;
using ReconHub.Domain.Transactions;

namespace ReconHub.Application.Abstractions.Persistence;

// All repositories operate within the current tenant; implementations enforce it.

public interface IIntegrationEventRepository
{
    Task<IntegrationEvent?> FindByIdempotencyKeyAsync(Guid sourceSystemId, string idempotencyKey, CancellationToken cancellationToken);

    void Add(IntegrationEvent integrationEvent);
}

public interface IFlowDefinitionRepository
{
    /// <summary>Active flows in which the given event type from the given system is the trigger or the follow-up.</summary>
    Task<IReadOnlyList<FlowDefinition>> ListActiveForEventAsync(Guid sourceSystemId, string eventType, CancellationToken cancellationToken);

    /// <summary>Flows by id, active or not (existing transactions keep being evaluated after a flow is deactivated).</summary>
    Task<IReadOnlyList<FlowDefinition>> ListByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken);
}

public interface ITransactionRepository
{
    /// <summary>The transaction for a flow and business key, with its events.</summary>
    Task<Transaction?> FindByBusinessKeyAsync(Guid flowDefinitionId, string businessKey, CancellationToken cancellationToken);

    /// <summary>The transaction owning an exception, with its exceptions.</summary>
    Task<Transaction?> FindByExceptionIdAsync(Guid exceptionId, CancellationToken cancellationToken);

    /// <summary>Transactions whose evaluation is due, oldest first, with their events and exceptions.</summary>
    Task<IReadOnlyList<Transaction>> ListDueForEvaluationAsync(DateTimeOffset now, int batchSize, CancellationToken cancellationToken);

    Task<IReadOnlyList<Guid>> ListIdsForEventAsync(Guid integrationEventId, CancellationToken cancellationToken);

    void Add(Transaction transaction);
}

public interface IAuditLog
{
    void Add(AuditEntry entry);
}

/// <summary>Read-side queries that project straight to DTOs.</summary>
public interface IExceptionQueries
{
    Task<PagedResult<ExceptionDto>> ListAsync(ListExceptionsQuery query, CancellationToken cancellationToken);

    Task<ExceptionDto?> GetAsync(Guid exceptionId, CancellationToken cancellationToken);
}
