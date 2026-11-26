using ReconHub.Domain.Events;
using ReconHub.Domain.Exceptions;

namespace ReconHub.Domain.Detection;

/// <summary>
/// Raises <see cref="ExceptionType.Mismatch"/> when any of the flow's match fields differ between the earliest
/// trigger and the earliest follow-up. Only compares when both events succeeded; failures are reported by
/// <see cref="FailedEventRule"/>. The finding refers to the follow-up event.
/// </summary>
public sealed class PayloadMismatchRule : IDetectionRule
{
    public ExceptionType Type => ExceptionType.Mismatch;

    public IEnumerable<DetectionFinding> Evaluate(DetectionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.Flow.MatchFields.Count == 0
            || context.Trigger is not { Status: EventStatus.Success } trigger
            || context.FollowUp is not { Status: EventStatus.Success } followUp)
        {
            return [];
        }

        var differences = PayloadFieldComparer.Compare(trigger.Payload, followUp.Payload, context.Flow.MatchFields);
        if (differences.Count == 0)
        {
            return [];
        }

        var described = string.Join(
            "; ",
            differences.Select(d => $"{d.Field}: {trigger.EventType}={d.TriggerValue}, {followUp.EventType}={d.FollowUpValue}"));

        return [new DetectionFinding(Type, followUp.Id, $"Payload fields differ: {described}.")];
    }
}
