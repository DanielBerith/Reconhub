using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using ReconHub.Api.Auth;
using ReconHub.Api.Http;
using ReconHub.Application.Common;
using ReconHub.Application.Exceptions;
using ReconHub.Domain.Exceptions;

namespace ReconHub.Api.Endpoints;

internal static class ExceptionEndpoints
{
    public static IEndpointRouteBuilder MapExceptionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/exceptions")
            .WithTags("Exceptions")
            .RequireAuthorization(AuthorizationPolicies.Operator)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapGet("", ListAsync)
            .WithName("ListExceptions")
            .WithSummary("List reconciliation exceptions")
            .WithDescription("Filter by type, status and detection time range [from, to). Newest first, paged.");

        group.MapPost("{id:guid}/resolve", ResolveAsync)
            .WithName("ResolveException")
            .WithSummary("Resolve an exception")
            .WithDescription("Marks an open exception as resolved with a mandatory note and writes an audit entry.")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }

    private static async Task<Results<Ok<PagedResult<ExceptionDto>>, ValidationProblem>> ListAsync(
        [AsParameters] ListExceptionsParameters parameters,
        ListExceptionsHandler handler,
        CancellationToken cancellationToken)
    {
        var query = new ListExceptionsQuery(
            parameters.Type,
            parameters.Status,
            parameters.From,
            parameters.To,
            parameters.Page ?? 1,
            parameters.PageSize ?? ListExceptionsQuery.DefaultPageSize);

        var result = await handler.HandleAsync(query, cancellationToken);
        if (!result.IsSuccess)
        {
            return result.Error.ToValidationProblem();
        }

        return TypedResults.Ok(result.Value);
    }

    private static async Task<Results<Ok<ExceptionDto>, ValidationProblem, ProblemHttpResult>> ResolveAsync(
        Guid id,
        ResolveExceptionRequest request,
        ResolveExceptionHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new ResolveExceptionCommand(id, request.Note), cancellationToken);
        if (result.IsSuccess)
        {
            return TypedResults.Ok(result.Value);
        }

        if (result.Error.Type == ErrorType.Validation)
        {
            return result.Error.ToValidationProblem();
        }

        return result.Error.ToProblem();
    }

    internal sealed record ListExceptionsParameters(
        [FromQuery(Name = "type")] ExceptionType? Type,
        [FromQuery(Name = "status")] ExceptionStatus? Status,
        [FromQuery(Name = "from")] DateTimeOffset? From,
        [FromQuery(Name = "to")] DateTimeOffset? To,
        [FromQuery(Name = "page")] int? Page,
        [FromQuery(Name = "pageSize")] int? PageSize);
}
