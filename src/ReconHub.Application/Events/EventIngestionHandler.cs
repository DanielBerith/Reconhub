using System.Text.Json;
using FluentValidation;
using Microsoft.Extensions.Logging;
using ReconHub.Application.Abstractions;
using ReconHub.Application.Abstractions.Persistence;
using ReconHub.Application.Common;
using ReconHub.Domain.Events;
using ReconHub.Domain.Transactions;

namespace ReconHub.Application.Events;

/// <summary>
/// Stores a reported event (idempotent per source system and idempotency key) and attaches it to the
/// transaction of every active flow it takes part in, creating transactions as needed.
/// Detection happens later, in the background job.
/// </summary>
public sealed class EventIngestionHandler(
    IValidator<IngestEventRequest> validator,
    ITenantContext tenantContext,
    IIntegrationEventRepository events,
    IFlowDefinitionRepository flows,
    ITransactionRepository transactions,
    IUnitOfWork unitOfWork,
    TimeProvider clock,
    ILogger<EventIngestionHandler> logger)
{
    // Concurrent requests can race on the idempotency key or on creating the same transaction.
    // The loser hits a unique index or a row version check; retrying re-reads the winner's data.
    private const int MaxAttempts = 3;

    public async Task<Result<IngestEventResponse>> HandleAsync(IngestEventCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var validation = await validator.ValidateAsync(command.Request, cancellationToken);
        if (!validation.IsValid)
        {
            return Result.Failure<IngestEventResponse>(validation.ToError());
        }

        for (var attempt = 1; ; attempt++)
        {
            try
            {
                return Result.Success(await IngestAsync(command, cancellationToken));
            }
            catch (Exception ex) when (ex is UniqueConstraintViolationException or ConcurrencyConflictException && attempt < MaxAttempts)
            {
                logger.LogDebug(ex, "Concurrent write while ingesting event {IdempotencyKey}; retrying (attempt {Attempt})", command.Request.IdempotencyKey, attempt);
                unitOfWork.Reset();
            }
        }
    }

    private async Task<IngestEventResponse> IngestAsync(IngestEventCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;
        var idempotencyKey = request.IdempotencyKey.Trim();

        var existing = await events.FindByIdempotencyKeyAsync(command.SourceSystemId, idempotencyKey, cancellationToken);
        if (existing is not null)
        {
            var existingTransactionIds = await transactions.ListIdsForEventAsync(existing.Id, cancellationToken);
            return new IngestEventResponse(existing.Id, Replayed: true, existingTransactionIds);
        }

        var tenantId = tenantContext.RequireTenantId();
        var now = clock.GetUtcNow();
        var payload = request.Payload.ValueKind == JsonValueKind.Object ? request.Payload.GetRawText() : "{}";

        var integrationEvent = new IntegrationEvent(
            tenantId,
            command.SourceSystemId,
            request.EventType,
            request.BusinessKey,
            request.OccurredAt,
            request.Status,
            payload,
            idempotencyKey,
            receivedAt: now);
        events.Add(integrationEvent);

        var matchingFlows = await flows.ListActiveForEventAsync(command.SourceSystemId, integrationEvent.EventType, cancellationToken);
        var transactionIds = new List<Guid>(matchingFlows.Count);

        foreach (var flow in matchingFlows)
        {
            var transaction = await transactions.FindByBusinessKeyAsync(flow.Id, integrationEvent.BusinessKey, cancellationToken);
            if (transaction is null)
            {
                transaction = new Transaction(tenantId, flow.Id, integrationEvent.BusinessKey, now);
                transactions.Add(transaction);
            }

            transaction.AttachEvent(integrationEvent, flow, now);
            transactionIds.Add(transaction.Id);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        if (matchingFlows.Count == 0)
        {
            logger.LogInformation(
                "Event {EventType} from source system {SourceSystemId} matches no active flow; stored without reconciliation",
                integrationEvent.EventType,
                command.SourceSystemId);
        }

        return new IngestEventResponse(integrationEvent.Id, Replayed: false, transactionIds);
    }
}
