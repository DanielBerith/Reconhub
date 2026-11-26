using FluentValidation;
using ReconHub.Application.Abstractions.Persistence;
using ReconHub.Application.Common;
using ReconHub.Domain.Exceptions;

namespace ReconHub.Application.Exceptions;

/// <summary>Filters exceptions by type, status and detection time range [From, To); results are newest first.</summary>
public sealed record ListExceptionsQuery(
    ExceptionType? Type = null,
    ExceptionStatus? Status = null,
    DateTimeOffset? From = null,
    DateTimeOffset? To = null,
    int Page = 1,
    int PageSize = ListExceptionsQuery.DefaultPageSize)
{
    public const int DefaultPageSize = 50;
    public const int MaxPageSize = 200;
}

public sealed class ListExceptionsQueryValidator : AbstractValidator<ListExceptionsQuery>
{
    public ListExceptionsQueryValidator()
    {
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.Status).IsInEnum();
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, ListExceptionsQuery.MaxPageSize);
        RuleFor(x => x.To)
            .GreaterThan(x => x.From)
            .When(x => x.From.HasValue && x.To.HasValue)
            .WithMessage("'To' must be later than 'From'.");
    }
}

public sealed class ListExceptionsHandler(IValidator<ListExceptionsQuery> validator, IExceptionQueries queries)
{
    public async Task<Result<PagedResult<ExceptionDto>>> HandleAsync(ListExceptionsQuery query, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(query, cancellationToken);
        if (!validation.IsValid)
        {
            return Result.Failure<PagedResult<ExceptionDto>>(validation.ToError());
        }

        return Result.Success(await queries.ListAsync(query, cancellationToken));
    }
}
