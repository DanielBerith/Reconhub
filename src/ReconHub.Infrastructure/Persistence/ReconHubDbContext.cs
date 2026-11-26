using System.Reflection;
using Microsoft.EntityFrameworkCore;
using ReconHub.Application.Abstractions;
using ReconHub.Domain.Auditing;
using ReconHub.Domain.Common;
using ReconHub.Domain.Events;
using ReconHub.Domain.Exceptions;
using ReconHub.Domain.Flows;
using ReconHub.Domain.SourceSystems;
using ReconHub.Domain.Tenants;
using ReconHub.Domain.Transactions;

namespace ReconHub.Infrastructure.Persistence;

public sealed class ReconHubDbContext(DbContextOptions<ReconHubDbContext> options, ITenantContext tenantContext)
    : DbContext(options)
{
    public const string Schema = "recon";

    /// <summary>Name of the global query filter that restricts every tenant-scoped entity to the current tenant.</summary>
    public const string TenantFilter = "TenantIsolation";

    private static readonly MethodInfo ApplyTenantFilterMethod =
        typeof(ReconHubDbContext).GetMethod(nameof(ApplyTenantFilter), BindingFlags.Instance | BindingFlags.NonPublic)!;

    public DbSet<Tenant> Tenants => Set<Tenant>();

    public DbSet<SourceSystem> SourceSystems => Set<SourceSystem>();

    public DbSet<IntegrationEvent> IntegrationEvents => Set<IntegrationEvent>();

    public DbSet<FlowDefinition> FlowDefinitions => Set<FlowDefinition>();

    public DbSet<Transaction> Transactions => Set<Transaction>();

    public DbSet<ReconException> ReconExceptions => Set<ReconException>();

    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();

    // Referenced by the query filter; EF Core re-reads it for each query on each context instance.
    private Guid? CurrentTenantId => tenantContext.TenantId;

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        EnforceTenantIsolation();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        EnforceTenantIsolation();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ReconHubDbContext).Assembly);

        foreach (var clrType in modelBuilder.Model.GetEntityTypes().Select(e => e.ClrType).ToList())
        {
            if (typeof(Entity).IsAssignableFrom(clrType))
            {
                // Ids are created by the domain. Without this, EF would treat a new child entity that has an id
                // (e.g. a ReconException added to a Transaction) as existing and try to UPDATE it.
                modelBuilder.Entity(clrType).Property(nameof(Entity.Id)).ValueGeneratedNever();
            }

            if (typeof(ITenantScoped).IsAssignableFrom(clrType))
            {
                ApplyTenantFilterMethod.MakeGenericMethod(clrType).Invoke(this, [modelBuilder]);
            }
        }

        // Records are never hard-deleted; restrict avoids SQL Server's multiple-cascade-path errors.
        foreach (var foreignKey in modelBuilder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
        {
            foreignKey.DeleteBehavior = DeleteBehavior.Restrict;
        }
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Enums are stored as readable strings for support staff querying the database.
        configurationBuilder.Properties<Enum>().HaveConversion<string>().HaveMaxLength(32);
    }

    private void ApplyTenantFilter<TEntity>(ModelBuilder modelBuilder)
        where TEntity : class, ITenantScoped =>
        modelBuilder.Entity<TEntity>().HasQueryFilter(TenantFilter, e => e.TenantId == CurrentTenantId);

    /// <summary>Defense in depth: refuse to write tenant-scoped rows that do not belong to the current tenant.</summary>
    private void EnforceTenantIsolation()
    {
        ChangeTracker.DetectChanges();

        foreach (var entry in ChangeTracker.Entries<ITenantScoped>())
        {
            if (entry.State is EntityState.Unchanged or EntityState.Detached)
            {
                continue;
            }

            if (CurrentTenantId is not { } tenantId || entry.Entity.TenantId != tenantId)
            {
                throw new InvalidOperationException(
                    $"Blocked {entry.State} of {entry.Metadata.ClrType.Name}: it does not belong to the current tenant.");
            }
        }
    }
}
