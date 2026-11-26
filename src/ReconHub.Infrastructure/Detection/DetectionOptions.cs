namespace ReconHub.Infrastructure.Detection;

public sealed class DetectionOptions
{
    public const string SectionName = "Detection";

    /// <summary>Run the background worker. Disabled in integration tests, which drive cycles directly.</summary>
    public bool Enabled { get; set; } = true;

    public TimeSpan Interval { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>Transactions evaluated per database round trip.</summary>
    public int BatchSize { get; set; } = 200;

    /// <summary>Upper bound on batches per tenant per cycle, so one busy tenant cannot starve the others.</summary>
    public int MaxBatchesPerTenant { get; set; } = 50;
}
