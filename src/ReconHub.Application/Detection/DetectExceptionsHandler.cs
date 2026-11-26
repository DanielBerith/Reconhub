using Microsoft.Extensions.Logging;
using ReconHub.Application.Abstractions.Persistence;
using ReconHub.Domain.Auditing;
using ReconHub.Domain.Detection;
using ReconHub.Domain.Exceptions;

namespace ReconHub.Application.Detection;

public sealed record DetectionBatchResult(int Evaluated, int Raised)
{
    public static DetectionBatchResult Empty { get; } = new(0, 0);
}

/// <summary>
/// Evaluates one batch of due transactions for the current tenant: runs the detection rules,
/// raises new exceptions and writes an audit entry for each.
/// </summary>
public sealed class DetectExceptionsHandler(
    ITransactionRepository transactions,
    IFlowDefinitionRepository flows,
    IAuditLog auditLog,
    IUnitOfWork unitOfWork,
    TransactionEvaluator evaluator,
    TimeProvider clock,
    ILogger<DetectExceptionsHandler> logger)
{
    public async Task<DetectionBatchResult> HandleAsync(int batchSize, CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(batchSize, 1);

        var now = clock.GetUtcNow();
        var due = await transactions.ListDueForEvaluationAsync(now, batchSize, cancellationToken);
        if (due.Count == 0)
        {
            return DetectionBatchResult.Empty;
        }

        var flowIds = due.Select(t => t.FlowDefinitionId).Distinct().ToList();
        var flowsById = (await flows.ListByIdsAsync(flowIds, cancellationToken)).ToDictionary(f => f.Id);

        var raised = new List<(ReconException Exception, string BusinessKey)>();
        foreach (var transaction in due)
        {
            if (!flowsById.TryGetValue(transaction.FlowDefinitionId, out var flow))
            {
                logger.LogError("Flow {FlowDefinitionId} of transaction {TransactionId} not found", transaction.FlowDefinitionId, transaction.Id);
                continue;
            }

            foreach (var exception in evaluator.Evaluate(transaction, flow, now))
            {
                auditLog.Add(AuditEntry.ExceptionRaised(exception, now));
                raised.Add((exception, transaction.BusinessKey));
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        foreach (var (exception, businessKey) in raised)
        {
            logger.LogInformation(
                "Raised {ExceptionType} exception {ExceptionId} for transaction {TransactionId} (business key {BusinessKey})",
                exception.Type,
                exception.Id,
                exception.TransactionId,
                businessKey);
        }

        return new DetectionBatchResult(due.Count, raised.Count);
    }
}
