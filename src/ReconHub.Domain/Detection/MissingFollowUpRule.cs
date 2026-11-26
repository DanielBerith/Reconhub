using ReconHub.Domain.Exceptions;

namespace ReconHub.Domain.Detection;

/// <summary>
/// Raises <see cref="ExceptionType.Missing"/> when the follow-up was not received by the deadline
/// (trigger time + completion window), or when it was received but occurred after the deadline.
/// The finding refers to the unanswered trigger event.
/// </summary>
public sealed class MissingFollowUpRule : IDetectionRule
{
    public ExceptionType Type => ExceptionType.Missing;

    public IEnumerable<DetectionFinding> Evaluate(DetectionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.Trigger is not { } trigger || context.Deadline is not { } deadline)
        {
            return [];
        }

        var flow = context.Flow;

        if (context.FollowUp is not { } followUp)
        {
            return context.Now >= deadline
                ? [new DetectionFinding(
                    Type,
                    trigger.Id,
                    $"{flow.FollowUpEventType} was not received within {DetectionText.Duration(flow.CompletionWindow)} " +
                    $"of {trigger.EventType} (deadline {DetectionText.Timestamp(deadline)}).")]
                : [];
        }

        return followUp.OccurredAt > deadline
            ? [new DetectionFinding(
                Type,
                trigger.Id,
                $"{followUp.EventType} arrived late: it occurred {DetectionText.Duration(followUp.OccurredAt - deadline)} " +
                $"after the deadline ({DetectionText.Timestamp(deadline)}).")]
            : [];
    }
}
