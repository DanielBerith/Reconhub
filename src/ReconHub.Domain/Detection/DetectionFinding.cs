using ReconHub.Domain.Exceptions;

namespace ReconHub.Domain.Detection;

/// <summary>A problem found by a rule. <paramref name="EventId"/> is the event the finding is about.</summary>
public sealed record DetectionFinding(ExceptionType Type, Guid EventId, string Detail);
