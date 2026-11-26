using ReconHub.Domain.Events;
using ReconHub.Domain.Flows;

namespace ReconHub.Domain.Detection;

/// <summary>Everything a rule may look at, with events ordered by business time.</summary>
public sealed class DetectionContext
{
    public DetectionContext(FlowDefinition flow, IEnumerable<IntegrationEvent> events, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(flow);
        ArgumentNullException.ThrowIfNull(events);

        Flow = flow;
        Now = now;
        Events = [.. events.OrderBy(e => e.OccurredAt).ThenBy(e => e.ReceivedAt).ThenBy(e => e.Id)];
        Triggers = [.. Events.Where(e => flow.RoleOf(e) == FlowRole.Trigger)];
        FollowUps = [.. Events.Where(e => flow.RoleOf(e) == FlowRole.FollowUp)];
    }

    public FlowDefinition Flow { get; }

    public DateTimeOffset Now { get; }

    public IReadOnlyList<IntegrationEvent> Events { get; }

    public IReadOnlyList<IntegrationEvent> Triggers { get; }

    public IReadOnlyList<IntegrationEvent> FollowUps { get; }

    /// <summary>The earliest trigger event, if any.</summary>
    public IntegrationEvent? Trigger => Triggers.Count > 0 ? Triggers[0] : null;

    /// <summary>The earliest follow-up event, if any.</summary>
    public IntegrationEvent? FollowUp => FollowUps.Count > 0 ? FollowUps[0] : null;

    /// <summary>Trigger time plus the completion window; null without a trigger.</summary>
    public DateTimeOffset? Deadline => Trigger?.OccurredAt + Flow.CompletionWindow;
}
