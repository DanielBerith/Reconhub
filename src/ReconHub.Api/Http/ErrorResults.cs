using Microsoft.AspNetCore.Http.HttpResults;
using ReconHub.Application.Common;

namespace ReconHub.Api.Http;

/// <summary>Maps use-case errors to RFC 9457 problem details. The only HTTP-specific error handling in the API.</summary>
internal static class ErrorResults
{
    public static ValidationProblem ToValidationProblem(this Error error) =>
        TypedResults.ValidationProblem(
            error.ValidationErrors,
            title: error.Message,
            extensions: new Dictionary<string, object?> { ["code"] = error.Code });

    public static ProblemHttpResult ToProblem(this Error error) =>
        TypedResults.Problem(
            detail: error.Message,
            statusCode: error.Type switch
            {
                ErrorType.NotFound => StatusCodes.Status404NotFound,
                ErrorType.Conflict => StatusCodes.Status409Conflict,
                _ => StatusCodes.Status400BadRequest,
            },
            extensions: new Dictionary<string, object?> { ["code"] = error.Code });
}
