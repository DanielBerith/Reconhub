using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ReconHub.Application.Detection;
using ReconHub.Infrastructure.Persistence;
using ReconHub.Infrastructure.Tenancy;

namespace ReconHub.Infrastructure.Detection;

/// <summary>
/// Runs detection for every active tenant. Each batch gets its own DI scope with the tenant set, so the
/// global query filter isolates the data exactly as it does for API requests. A failure in one tenant
/// is logged and does not stop the others.
/// </summary>
internal sealed class DetectionCycle(
    IServiceScopeFactory scopeFactory,
    IOptions<DetectionOptions> options,
    ILogger<DetectionCycle> logger) : IDetectionCycle
{
    public async Task<DetectionCycleSummary> RunAsync(CancellationToken cancellationToken)
    {
        var settings = options.Value;
        var tenantIds = await ListActiveTenantIdsAsync(cancellationToken);
        int evaluated = 0, raised = 0, failedTenants = 0;

        foreach (var tenantId in tenantIds)
        {
            using var logScope = logger.BeginScope(new Dictionary<string, object> { ["TenantId"] = tenantId });
            try
            {
                for (var batch = 0; batch < settings.MaxBatchesPerTenant; batch++)
                {
                    await using var scope = scopeFactory.CreateAsyncScope();
                    scope.ServiceProvider.GetRequiredService<TenantContext>().SetTenant(tenantId);
                    var handler = scope.ServiceProvider.GetRequiredService<DetectExceptionsHandler>();

                    var result = await handler.HandleAsync(settings.BatchSize, cancellationToken);
                    evaluated += result.Evaluated;
                    raised += result.Raised;

                    if (result.Evaluated < settings.BatchSize)
                    {
                        break;
                    }
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Isolate tenants: whatever went wrong here is retried on the next cycle.
                failedTenants++;
                logger.LogError(ex, "Detection failed for tenant {TenantId}", tenantId);
            }
        }

        return new DetectionCycleSummary(tenantIds.Count, evaluated, raised, failedTenants);
    }

    private async Task<IReadOnlyList<Guid>> ListActiveTenantIdsAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ReconHubDbContext>();
        return await db.Tenants.AsNoTracking().Where(t => t.IsActive).Select(t => t.Id).ToListAsync(cancellationToken);
    }
}
