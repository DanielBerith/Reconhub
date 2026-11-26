using ReconHub.Domain.Common;
using ReconHub.Domain.Exceptions;

namespace ReconHub.Domain.Auditing;

/// <summary>An append-only record of who did what to which exception, and when.</summary>
public sealed class AuditEntry : Entity, ITenantScoped
{
    public const string SystemActor = "system";
    public const int ActorMaxLength = 200;
    public const int DetailsMaxLength = 2000;

    private AuditEntry(
        Guid tenantId,
        Guid reconExceptionId,
        AuditAction action,
        string actor,
        string? details,
        DateTimeOffset occurredAt)
    {
        TenantId = Guard.NotEmpty(tenantId, nameof(tenantId));
        ReconExceptionId = Guard.NotEmpty(reconExceptionId, nameof(reconExceptionId));
        Action = action;
        Actor = Guard.Text(actor, ActorMaxLength, nameof(actor));
        Details = details is { Length: > DetailsMaxLength } ? details[..DetailsMaxLength] : details;
        OccurredAt = occurredAt;
    }

    private AuditEntry() => Actor = null!; // EF Core

    public Guid TenantId { get; private set; }

    public Guid ReconExceptionId { get; private set; }

    public AuditAction Action { get; private set; }

    public string Actor { get; private set; }

    public string? Details { get; private set; }

    public DateTimeOffset OccurredAt { get; private set; }

    public static AuditEntry ExceptionRaised(ReconException exception, DateTimeOffset occurredAt)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return new AuditEntry(exception.TenantId, exception.Id, AuditAction.ExceptionRaised, SystemActor, exception.Detail, occurredAt);
    }

    public static AuditEntry ExceptionResolved(ReconException exception, DateTimeOffset occurredAt)
    {
        ArgumentNullException.ThrowIfNull(exception);
        if (exception.Status != ExceptionStatus.Resolved || exception.ResolvedBy is null)
        {
            throw new DomainException($"Exception {exception.Id} is not resolved.");
        }

        return new AuditEntry(
            exception.TenantId,
            exception.Id,
            AuditAction.ExceptionResolved,
            exception.ResolvedBy,
            exception.ResolutionNote,
            occurredAt);
    }
}
