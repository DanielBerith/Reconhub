using ReconHub.Domain.Events;
using ReconHub.Domain.Exceptions;

namespace ReconHub.Domain.Detection;

/// <summary>Raises <see cref="ExceptionType.Failed"/> for every event reported with <see cref="EventStatus.Failed"/>.</summary>
public sealed class FailedEventRule : IDetectionRule
{
    public ExceptionType Type => ExceptionType.Failed;

    public IEnumerable<DetectionFinding> Evaluate(DetectionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return
        [
            .. context.Events
                .Where(e => e.Status == EventStatus.Failed)
                .Select(e => new DetectionFinding(
                    Type,
                    e.Id,
                    $"{e.EventType} was reported as Failed by the source system (occurred {DetectionText.Timestamp(e.OccurredAt)}).")),
        ];
    }
}
