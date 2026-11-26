namespace ReconHub.Domain.Flows;

/// <summary>One expected step of a flow: an event type reported by a specific source system.</summary>
public sealed record FlowStep(Guid SourceSystemId, string EventType)
{
    public bool Matches(Guid sourceSystemId, string eventType) =>
        SourceSystemId == sourceSystemId && string.Equals(EventType, eventType, StringComparison.OrdinalIgnoreCase);
}
