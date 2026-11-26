using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ReconHub.Api.Endpoints;

public sealed record HealthResponse(HealthStatus Status, double TotalDurationMs, IReadOnlyList<HealthCheckResult> Checks);

public sealed record HealthCheckResult(string Name, HealthStatus Status, string? Description, double DurationMs);

internal static class HealthEndpoints
{
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        // Probes for orchestrators: liveness runs no checks, readiness checks the database.
        app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false }).AllowAnonymous();
        app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = c => c.Tags.Contains("ready") }).AllowAnonymous();

        // A typed, documented variant for humans and the web app (health-check endpoints are not in OpenAPI).
        app.MapGet("/api/health", GetHealthAsync)
            .WithTags("Health")
            .WithName("GetHealth")
            .WithSummary("Service health")
            .WithDescription("Always returns 200; the status field reports the actual health.")
            .AllowAnonymous();

        return app;
    }

    private static async Task<Ok<HealthResponse>> GetHealthAsync(HealthCheckService healthChecks, CancellationToken cancellationToken)
    {
        var report = await healthChecks.CheckHealthAsync(cancellationToken);

        // Exception details are deliberately omitted: this endpoint is anonymous.
        var checks = report.Entries
            .Select(entry => new HealthCheckResult(
                entry.Key,
                entry.Value.Status,
                entry.Value.Description,
                entry.Value.Duration.TotalMilliseconds))
            .ToList();

        return TypedResults.Ok(new HealthResponse(report.Status, report.TotalDuration.TotalMilliseconds, checks));
    }
}
