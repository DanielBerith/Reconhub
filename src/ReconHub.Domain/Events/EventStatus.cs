namespace ReconHub.Domain.Events;

/// <summary>Outcome of the business step as reported by the source system.</summary>
public enum EventStatus
{
    Success = 1,
    Failed = 2,
}
