using ReconHub.Domain.Common;

namespace ReconHub.Domain.Events;

/// <summary>A single fact reported by a source system. Immutable once recorded.</summary>
public sealed class IntegrationEvent : Entity, ITenantScoped
{
    public const int EventTypeMaxLength = 100;
    public const int BusinessKeyMaxLength = 200;
    public const int IdempotencyKeyMaxLength = 200;

    public IntegrationEvent(
        Guid tenantId,
        Guid sourceSystemId,
        string eventType,
        string businessKey,
        DateTimeOffset occurredAt,
        EventStatus status,
        string payload,
        string idempotencyKey,
        DateTimeOffset receivedAt)
    {
        TenantId = Guard.NotEmpty(tenantId, nameof(tenantId));
        SourceSystemId = Guard.NotEmpty(sourceSystemId, nameof(sourceSystemId));
        EventType = Guard.Text(eventType, EventTypeMaxLength, nameof(eventType));
        BusinessKey = Guard.Text(businessKey, BusinessKeyMaxLength, nameof(businessKey));
        IdempotencyKey = Guard.Text(idempotencyKey, IdempotencyKeyMaxLength, nameof(idempotencyKey));
        ArgumentNullException.ThrowIfNull(payload);
        Payload = payload;
        OccurredAt = occurredAt;
        Status = status;
        ReceivedAt = receivedAt;
    }

    private IntegrationEvent()
    {
        // EF Core
        EventType = null!;
        BusinessKey = null!;
        Payload = null!;
        IdempotencyKey = null!;
    }

    public Guid TenantId { get; private set; }

    public Guid SourceSystemId { get; private set; }

    public string EventType { get; private set; }

    /// <summary>The identifier shared by all events of one business transaction, e.g. an order number.</summary>
    public string BusinessKey { get; private set; }

    /// <summary>When the step happened in the source system (business time).</summary>
    public DateTimeOffset OccurredAt { get; private set; }

    public EventStatus Status { get; private set; }

    /// <summary>The raw JSON object supplied by the source system.</summary>
    public string Payload { get; private set; }

    public string IdempotencyKey { get; private set; }

    /// <summary>When ReconHub recorded the event (system time).</summary>
    public DateTimeOffset ReceivedAt { get; private set; }
}
