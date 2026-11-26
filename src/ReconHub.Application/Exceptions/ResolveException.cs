using System.ComponentModel.DataAnnotations;
using FluentValidation;
using ReconHub.Application.Abstractions;
using ReconHub.Application.Abstractions.Persistence;
using ReconHub.Application.Common;
using ReconHub.Domain.Auditing;
using ReconHub.Domain.Exceptions;

namespace ReconHub.Application.Exceptions;

public sealed record ResolveExceptionRequest
{
    /// <summary>What was done to resolve the exception. Required for the audit trail.</summary>
    [Required]
    public string Note { get; init; } = string.Empty;
}

public sealed record ResolveExceptionCommand(Guid ExceptionId, string Note);

public sealed class ResolveExceptionCommandValidator : AbstractValidator<ResolveExceptionCommand>
{
    public ResolveExceptionCommandValidator()
    {
        RuleFor(x => x.Note)
            .NotEmpty()
            .Must(note => !string.IsNullOrWhiteSpace(note))
            .WithMessage("'Note' must not be empty.")
            .MaximumLength(ReconException.ResolutionNoteMaxLength);
    }
}

/// <summary>Resolves an open exception with a note and records who did it in the audit log.</summary>
public sealed class ResolveExceptionHandler(
    IValidator<ResolveExceptionCommand> validator,
    ITransactionRepository transactions,
    IAuditLog auditLog,
    IExceptionQueries queries,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    TimeProvider clock)
{
    public async Task<Result<ExceptionDto>> HandleAsync(ResolveExceptionCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            return Result.Failure<ExceptionDto>(validation.ToError());
        }

        var transaction = await transactions.FindByExceptionIdAsync(command.ExceptionId, cancellationToken);
        var exception = transaction?.Exceptions.SingleOrDefault(e => e.Id == command.ExceptionId);
        if (transaction is null || exception is null)
        {
            return Result.Failure<ExceptionDto>(Error.NotFound("exception_not_found", $"Exception {command.ExceptionId} was not found."));
        }

        if (exception.Status == ExceptionStatus.Resolved)
        {
            return Result.Failure<ExceptionDto>(Error.Conflict("exception_already_resolved", $"Exception {command.ExceptionId} is already resolved."));
        }

        var now = clock.GetUtcNow();
        transaction.ResolveException(exception.Id, command.Note, currentUser.Name, now);
        auditLog.Add(AuditEntry.ExceptionResolved(exception, now));

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (ConcurrencyConflictException)
        {
            return Result.Failure<ExceptionDto>(Error.Conflict(
                "concurrent_update",
                "The transaction was changed by another operation. Reload and try again."));
        }

        var dto = await queries.GetAsync(exception.Id, cancellationToken)
            ?? throw new InvalidOperationException($"Exception {exception.Id} disappeared after being resolved.");
        return Result.Success(dto);
    }
}
