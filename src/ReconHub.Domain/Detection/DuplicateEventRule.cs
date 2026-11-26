using ReconHub.Domain.Exceptions;

namespace ReconHub.Domain.Detection;

/// <summary>
/// Raises <see cref="ExceptionType.Duplicate"/> when the same source system reports the same event type more than
/// once for one business key. The earliest occurrence is the original; each later one gets a finding.
/// Replays with the same idempotency key never reach this rule because they are not stored twice.
/// </summary>
public sealed class DuplicateEventRule : IDetectionRule
{
    public ExceptionType Type => ExceptionType.Duplicate;

    public IEnumerable<DetectionFinding> Evaluate(DetectionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var findings = new List<DetectionFinding>();
        var groups = context.Events.GroupBy(e => (e.SourceSystemId, EventType: e.EventType.ToUpperInvariant()));

        foreach (var group in groups)
        {
            var occurrences = group.ToList();
            var original = occurrences[0];

            foreach (var duplicate in occurrences.Skip(1))
            {
                findings.Add(new DetectionFinding(
                    Type,
                    duplicate.Id,
                    $"{duplicate.EventType} was reported again for business key '{duplicate.BusinessKey}' " +
                    $"(idempotency key '{duplicate.IdempotencyKey}'); the original had idempotency key '{original.IdempotencyKey}'."));
            }
        }

        return findings;
    }
}
