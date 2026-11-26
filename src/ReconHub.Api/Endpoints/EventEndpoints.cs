using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using ReconHub.Api.Auth;
using ReconHub.Api.Http;
using ReconHub.Application.Events;

namespace ReconHub.Api.Endpoints;

internal static class EventEndpoints
{
    private const long MaxRequestBodyBytes = 256 * 1024;

    public static IEndpointRouteBuilder MapEventEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/events", IngestAsync)
            .WithTags("Events")
            .WithName("IngestEvent")
            .WithSummary("Report an integration event")
            .WithDescription(
                "Called by source systems, authenticated with the X-Api-Key header. Returns 201 for a new event and " +
                "200 with the original event when the idempotency key was already used by this source system.")
            .RequireAuthorization(AuthorizationPolicies.SourceSystem)
            .WithMetadata(new RequestSizeLimitAttribute(MaxRequestBodyBytes))
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        return app;
    }

    private static async Task<Results<Created<IngestEventResponse>, Ok<IngestEventResponse>, ValidationProblem>> IngestAsync(
        IngestEventRequest request,
        ClaimsPrincipal user,
        EventIngestionHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new IngestEventCommand(user.GetSourceSystemId(), request), cancellationToken);
        if (!result.IsSuccess)
        {
            return result.Error.ToValidationProblem();
        }

        return result.Value.Replayed
            ? TypedResults.Ok(result.Value)
            : TypedResults.Created((string?)null, result.Value);
    }
}
