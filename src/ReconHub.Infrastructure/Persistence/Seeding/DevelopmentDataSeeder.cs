using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ReconHub.Domain.Flows;
using ReconHub.Domain.SourceSystems;
using ReconHub.Domain.Tenants;
using ReconHub.Infrastructure.Security;
using ReconHub.Infrastructure.Tenancy;

namespace ReconHub.Infrastructure.Persistence.Seeding;

public sealed class SeedOptions
{
    public const string SectionName = "Seed";

    /// <summary>Seed demo data at startup. Development and test only; never enable in production.</summary>
    public bool Enabled { get; set; }

    public string? WmsApiKey { get; set; }

    public string? ErpApiKey { get; set; }
}

/// <summary>
/// Creates the demo tenant, WMS and ERP source systems, and the "ShipConfirm -> DeliveryPosted within 15 minutes,
/// quantity must match" flow. Idempotent. Lives outside migrations so production databases never receive demo keys.
/// </summary>
public sealed class DevelopmentDataSeeder(
    ReconHubDbContext db,
    TenantContext tenantContext,
    TimeProvider clock,
    IOptions<SeedOptions> options,
    ILogger<DevelopmentDataSeeder> logger)
{
    public static readonly Guid DemoTenantId = Guid.Parse("0195f0c0-0000-7000-8000-000000000001");
    public const string WmsName = "WMS";
    public const string ErpName = "ERP";
    public const string FlowName = "ShipConfirm -> DeliveryPosted";
    public const string TriggerEventType = "ShipConfirm";
    public const string FollowUpEventType = "DeliveryPosted";

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        var settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.WmsApiKey) || string.IsNullOrWhiteSpace(settings.ErpApiKey))
        {
            throw new InvalidOperationException("Seed:WmsApiKey and Seed:ErpApiKey must be set when Seed:Enabled is true.");
        }

        tenantContext.SetTenant(DemoTenantId);

        if (await db.Tenants.AnyAsync(t => t.Id == DemoTenantId, cancellationToken))
        {
            logger.LogDebug("Demo tenant {TenantId} already exists; skipping seed", DemoTenantId);
            return;
        }

        var now = clock.GetUtcNow();
        var tenant = new Tenant(DemoTenantId, "Demo Tenant", now);
        var wms = new SourceSystem(DemoTenantId, WmsName, SourceSystemType.Wms, ApiKeyHasher.Hash(settings.WmsApiKey), now);
        var erp = new SourceSystem(DemoTenantId, ErpName, SourceSystemType.Erp, ApiKeyHasher.Hash(settings.ErpApiKey), now);
        var flow = new FlowDefinition(
            DemoTenantId,
            FlowName,
            trigger: new FlowStep(wms.Id, TriggerEventType),
            followUp: new FlowStep(erp.Id, FollowUpEventType),
            completionWindow: TimeSpan.FromMinutes(15),
            matchFields: ["quantity"],
            createdAt: now);

        db.AddRange(tenant, wms, erp, flow);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Seeded demo tenant {TenantId} with {Wms}, {Erp} and flow '{FlowName}'", DemoTenantId, WmsName, ErpName, FlowName);
    }
}

public static class SeedingExtensions
{
    public static async Task SeedDevelopmentDataAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<DevelopmentDataSeeder>().SeedAsync(cancellationToken);
    }
}
