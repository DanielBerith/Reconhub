using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using ReconHub.Domain.Events;

namespace ReconHub.Application.Events;

/// <summary>An event reported by a source system. The source system itself is identified by its API key.</summary>
public sealed record IngestEventRequest
{
    /// <summary>The kind of business step, e.g. "ShipConfirm".</summary>
    [Required]
    public string EventType { get; init; } = string.Empty;

    /// <summary>Identifier shared by all events of one business transaction, e.g. an order number.</summary>
    [Required]
    public string BusinessKey { get; init; } = string.Empty;

    /// <summary>When the step happened in the source system.</summary>
    [Required]
    public DateTimeOffset OccurredAt { get; init; }

    [Required]
    public EventStatus Status { get; init; }

    /// <summary>Optional JSON object with business data, e.g. { "quantity": 10 }.</summary>
    public JsonElement Payload { get; init; }

    /// <summary>Unique per source system. Re-sending the same key returns the original event without reprocessing.</summary>
    [Required]
    public string IdempotencyKey { get; init; } = string.Empty;
}

public sealed record IngestEventCommand(Guid SourceSystemId, IngestEventRequest Request);

public sealed record IngestEventResponse(Guid EventId, bool Replayed, IReadOnlyList<Guid> TransactionIds);
