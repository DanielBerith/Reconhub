using ReconHub.Domain.Exceptions;
using ReconHub.Domain.Transactions;

namespace ReconHub.Application.Exceptions;

public sealed record ExceptionDto(
    Guid Id,
    ExceptionType Type,
    ExceptionStatus Status,
    string Detail,
    Guid TransactionId,
    string BusinessKey,
    TransactionState TransactionState,
    Guid FlowDefinitionId,
    string FlowName,
    Guid IntegrationEventId,
    DateTimeOffset DetectedAt,
    DateTimeOffset? ResolvedAt,
    string? ResolvedBy,
    string? ResolutionNote);
