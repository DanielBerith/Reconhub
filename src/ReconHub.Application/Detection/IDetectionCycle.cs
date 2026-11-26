namespace ReconHub.Application.Detection;

/// <summary>One full detection pass over every active tenant. Run by the background worker every interval.</summary>
public interface IDetectionCycle
{
    Task<DetectionCycleSummary> RunAsync(CancellationToken cancellationToken);
}

public sealed record DetectionCycleSummary(int Tenants, int Evaluated, int Raised, int FailedTenants);
