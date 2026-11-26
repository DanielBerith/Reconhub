using ReconHub.Domain.Exceptions;

namespace ReconHub.Domain.Detection;

/// <summary>
/// A pure reconciliation rule: given a transaction's events, its flow and the current time, it returns findings.
/// Rules never read clocks, databases or other state.
/// </summary>
public interface IDetectionRule
{
    ExceptionType Type { get; }

    IEnumerable<DetectionFinding> Evaluate(DetectionContext context);
}
