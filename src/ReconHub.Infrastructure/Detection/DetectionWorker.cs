using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ReconHub.Application.Detection;

namespace ReconHub.Infrastructure.Detection;

/// <summary>Runs a detection cycle at startup and then every <see cref="DetectionOptions.Interval"/>. Cycles never overlap.</summary>
internal sealed class DetectionWorker(
    IDetectionCycle cycle,
    IOptions<DetectionOptions> options,
    TimeProvider clock,
    ILogger<DetectionWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = options.Value;
        if (!settings.Enabled)
        {
            logger.LogInformation("Detection worker is disabled");
            return;
        }

        logger.LogInformation("Detection worker started; running every {Interval}", settings.Interval);
        using var timer = new PeriodicTimer(settings.Interval, clock);

        try
        {
            do
            {
                await RunCycleAsync(stoppingToken);
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Shutting down.
        }
    }

    private async Task RunCycleAsync(CancellationToken cancellationToken)
    {
        var startedAt = clock.GetTimestamp();
        try
        {
            var summary = await cycle.RunAsync(cancellationToken);
            var level = summary.Evaluated > 0 || summary.FailedTenants > 0 ? LogLevel.Information : LogLevel.Debug;
            logger.Log(
                level,
                "Detection cycle finished in {ElapsedMs:0} ms: {Tenants} tenants, {Evaluated} transactions evaluated, {Raised} exceptions raised, {FailedTenants} tenants failed",
                clock.GetElapsedTime(startedAt).TotalMilliseconds,
                summary.Tenants,
                summary.Evaluated,
                summary.Raised,
                summary.FailedTenants);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Typically the database is unreachable; try again on the next tick.
            logger.LogError(ex, "Detection cycle failed");
        }
    }
}
