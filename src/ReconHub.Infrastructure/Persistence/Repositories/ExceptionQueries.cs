using Microsoft.EntityFrameworkCore;
using ReconHub.Application.Abstractions.Persistence;
using ReconHub.Application.Common;
using ReconHub.Application.Exceptions;
using ReconHub.Domain.Exceptions;
using ReconHub.Domain.Transactions;

namespace ReconHub.Infrastructure.Persistence.Repositories;

internal sealed class ExceptionQueries(ReconHubDbContext db) : IExceptionQueries
{
    public async Task<PagedResult<ExceptionDto>> ListAsync(ListExceptionsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var rows = Rows();
        if (query.Type is { } type)
        {
            rows = rows.Where(r => r.Exception.Type == type);
        }

        if (query.Status is { } status)
        {
            rows = rows.Where(r => r.Exception.Status == status);
        }

        if (query.From is { } from)
        {
            rows = rows.Where(r => r.Exception.DetectedAt >= from);
        }

        if (query.To is { } to)
        {
            rows = rows.Where(r => r.Exception.DetectedAt < to);
        }

        var totalCount = await rows.CountAsync(cancellationToken);
        var items = await ToDto(rows
                .OrderByDescending(r => r.Exception.DetectedAt)
                .ThenByDescending(r => r.Exception.Id)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize))
            .ToListAsync(cancellationToken);

        return new PagedResult<ExceptionDto>(items, query.Page, query.PageSize, totalCount);
    }

    public Task<ExceptionDto?> GetAsync(Guid exceptionId, CancellationToken cancellationToken) =>
        ToDto(Rows().Where(r => r.Exception.Id == exceptionId)).FirstOrDefaultAsync(cancellationToken);

    private IQueryable<ExceptionRow> Rows() =>
        from exception in db.ReconExceptions.AsNoTracking()
        join transaction in db.Transactions on exception.TransactionId equals transaction.Id
        join flow in db.FlowDefinitions on transaction.FlowDefinitionId equals flow.Id
        select new ExceptionRow
        {
            Exception = exception,
            BusinessKey = transaction.BusinessKey,
            TransactionState = transaction.State,
            FlowDefinitionId = flow.Id,
            FlowName = flow.Name,
        };

    private static IQueryable<ExceptionDto> ToDto(IQueryable<ExceptionRow> rows) =>
        rows.Select(r => new ExceptionDto(
            r.Exception.Id,
            r.Exception.Type,
            r.Exception.Status,
            r.Exception.Detail,
            r.Exception.TransactionId,
            r.BusinessKey,
            r.TransactionState,
            r.FlowDefinitionId,
            r.FlowName,
            r.Exception.IntegrationEventId,
            r.Exception.DetectedAt,
            r.Exception.ResolvedAt,
            r.Exception.ResolvedBy,
            r.Exception.ResolutionNote));

    private sealed class ExceptionRow
    {
        public required ReconException Exception { get; init; }

        public required string BusinessKey { get; init; }

        public required TransactionState TransactionState { get; init; }

        public required Guid FlowDefinitionId { get; init; }

        public required string FlowName { get; init; }
    }
}
