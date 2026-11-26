using ReconHub.Domain.Common;
using ReconHub.Domain.Detection;
using ReconHub.Domain.Events;
using ReconHub.Domain.Exceptions;
using ReconHub.Domain.Flows;

namespace ReconHub.Domain.Transactions;

/// <summary>
/// The events that share a business key under one <see cref="FlowDefinition"/>, plus the exceptions raised for them.
/// Aggregate root: exceptions are raised and resolved only through this type so the state stays consistent.
/// </summary>
public sealed class Transaction : Entity, ITenantScoped
{
    private readonly List<IntegrationEvent> _events = [];
    private readonly List<ReconException> _exceptions = [];

    public Transaction(Guid tenantId, Guid flowDefinitionId, string businessKey, DateTimeOffset createdAt)
    {
        TenantId = Guard.NotEmpty(tenantId, nameof(tenantId));
        FlowDefinitionId = Guard.NotEmpty(flowDefinitionId, nameof(flowDefinitionId));
        BusinessKey = Guard.Text(businessKey, IntegrationEvent.BusinessKeyMaxLength, nameof(businessKey));
        State = TransactionState.Pending;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    private Transaction() => BusinessKey = null!; // EF Core

    public Guid TenantId { get; private set; }

    public Guid FlowDefinitionId { get; private set; }

    public string BusinessKey { get; private set; }

    public TransactionState State { get; private set; }

    /// <summary>Business time of the earliest trigger event.</summary>
    public DateTimeOffset? TriggerOccurredAt { get; private set; }

    /// <summary>Business time of the earliest follow-up event.</summary>
    public DateTimeOffset? FollowUpOccurredAt { get; private set; }

    /// <summary>Latest time the follow-up may occur: trigger time plus the flow's completion window.</summary>
    public DateTimeOffset? DueAt { get; private set; }

    public DateTimeOffset? CompletedAt { get; private set; }

    /// <summary>When the detection job should next look at this transaction; null when nothing can change without a new event.</summary>
    public DateTimeOffset? EvaluationDueAt { get; private set; }

    public DateTimeOffset? LastEvaluatedAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public IReadOnlyCollection<IntegrationEvent> Events => _events;

    public IReadOnlyCollection<ReconException> Exceptions => _exceptions;

    /// <summary>Adds an event to the transaction and marks it for evaluation. Returns false if already attached.</summary>
    public bool AttachEvent(IntegrationEvent integrationEvent, FlowDefinition flow, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);
        ArgumentNullException.ThrowIfNull(flow);

        if (flow.Id != FlowDefinitionId)
        {
            throw new DomainException($"Flow {flow.Id} does not govern transaction {Id}.");
        }

        if (integrationEvent.TenantId != TenantId)
        {
            throw new DomainException("An event cannot be attached to a transaction of another tenant.");
        }

        if (!string.Equals(integrationEvent.BusinessKey, BusinessKey, StringComparison.OrdinalIgnoreCase))
        {
            throw new DomainException($"Event business key '{integrationEvent.BusinessKey}' does not match transaction '{BusinessKey}'.");
        }

        var role = flow.RoleOf(integrationEvent)
            ?? throw new DomainException($"Event type '{integrationEvent.EventType}' is not part of flow '{flow.Name}'.");

        if (_events.Any(e => e.Id == integrationEvent.Id))
        {
            return false;
        }

        _events.Add(integrationEvent);

        var occurredAt = integrationEvent.OccurredAt;
        if (role == FlowRole.Trigger && (TriggerOccurredAt is null || occurredAt < TriggerOccurredAt))
        {
            TriggerOccurredAt = occurredAt;
            DueAt = occurredAt + flow.CompletionWindow;
        }
        else if (role == FlowRole.FollowUp && (FollowUpOccurredAt is null || occurredAt < FollowUpOccurredAt))
        {
            FollowUpOccurredAt = occurredAt;
        }

        EvaluationDueAt = now;
        UpdatedAt = now;
        return true;
    }

    /// <summary>
    /// Records the outcome of an evaluation: raises an exception for each finding not raised before
    /// (same type and event), schedules the next evaluation and recalculates the state.
    /// </summary>
    internal IReadOnlyList<ReconException> RecordEvaluation(
        IEnumerable<DetectionFinding> findings,
        DateTimeOffset? nextEvaluationAt,
        DateTimeOffset now)
    {
        var raised = new List<ReconException>();
        foreach (var finding in findings)
        {
            if (_exceptions.Any(e => e.Type == finding.Type && e.IntegrationEventId == finding.EventId))
            {
                continue;
            }

            var exception = new ReconException(TenantId, Id, finding.EventId, finding.Type, finding.Detail, now);
            _exceptions.Add(exception);
            raised.Add(exception);
        }

        EvaluationDueAt = nextEvaluationAt;
        LastEvaluatedAt = now;
        RecalculateState(now);
        UpdatedAt = now;
        return raised;
    }

    public ReconException ResolveException(Guid exceptionId, string note, string resolvedBy, DateTimeOffset now)
    {
        var exception = _exceptions.SingleOrDefault(e => e.Id == exceptionId)
            ?? throw new DomainException($"Exception {exceptionId} does not belong to transaction {Id}.");

        exception.Resolve(note, resolvedBy, now);
        RecalculateState(now);
        UpdatedAt = now;
        return exception;
    }

    private void RecalculateState(DateTimeOffset now)
    {
        if (_exceptions.Any(e => e.Status == ExceptionStatus.Open))
        {
            State = TransactionState.Exception;
            CompletedAt = null;
            return;
        }

        var bothStepsReceived = TriggerOccurredAt.HasValue && FollowUpOccurredAt.HasValue;
        var closedBySupport = _exceptions.Any(e => e.Type == ExceptionType.Missing && e.Status == ExceptionStatus.Resolved);

        if (bothStepsReceived || closedBySupport)
        {
            State = TransactionState.Completed;
            CompletedAt ??= now;
        }
        else
        {
            State = TransactionState.Pending;
            CompletedAt = null;
        }
    }
}
