using System.Diagnostics.CodeAnalysis;
using ReconHub.Domain.Common;

namespace ReconHub.Domain.Exceptions;

/// <summary>A reconciliation problem found by the detection job, for the support team to resolve.</summary>
[SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "Domain term; this is not a System.Exception.")]
public sealed class ReconException : Entity, ITenantScoped
{
    public const int DetailMaxLength = 2000;
    public const int ResolutionNoteMaxLength = 2000;
    public const int ResolvedByMaxLength = 200;

    internal ReconException(
        Guid tenantId,
        Guid transactionId,
        Guid integrationEventId,
        ExceptionType type,
        string detail,
        DateTimeOffset detectedAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(detail);
        TenantId = Guard.NotEmpty(tenantId, nameof(tenantId));
        TransactionId = Guard.NotEmpty(transactionId, nameof(transactionId));
        IntegrationEventId = Guard.NotEmpty(integrationEventId, nameof(integrationEventId));
        Type = type;
        Detail = detail.Length <= DetailMaxLength ? detail : detail[..(DetailMaxLength - 3)] + "...";
        Status = ExceptionStatus.Open;
        DetectedAt = detectedAt;
    }

    private ReconException() => Detail = null!; // EF Core

    public Guid TenantId { get; private set; }

    public Guid TransactionId { get; private set; }

    /// <summary>The event the finding is about (the failed, duplicated or mismatching event, or the unanswered trigger).</summary>
    public Guid IntegrationEventId { get; private set; }

    public ExceptionType Type { get; private set; }

    public ExceptionStatus Status { get; private set; }

    public string Detail { get; private set; }

    public DateTimeOffset DetectedAt { get; private set; }

    public DateTimeOffset? ResolvedAt { get; private set; }

    public string? ResolvedBy { get; private set; }

    public string? ResolutionNote { get; private set; }

    internal void Resolve(string note, string resolvedBy, DateTimeOffset now)
    {
        if (Status == ExceptionStatus.Resolved)
        {
            throw new DomainException($"Exception {Id} is already resolved.");
        }

        ResolutionNote = Guard.Text(note, ResolutionNoteMaxLength, nameof(note));
        ResolvedBy = Guard.Text(resolvedBy, ResolvedByMaxLength, nameof(resolvedBy));
        ResolvedAt = now;
        Status = ExceptionStatus.Resolved;
    }
}
