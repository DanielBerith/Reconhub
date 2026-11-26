using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReconHub.Domain.Events;
using ReconHub.Domain.Flows;
using ReconHub.Domain.Tenants;
using ReconHub.Domain.Transactions;

namespace ReconHub.Infrastructure.Persistence.Configurations;

internal sealed class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public const string RowVersion = "RowVersion";

    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable("Transactions");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.BusinessKey).HasMaxLength(IntegrationEvent.BusinessKeyMaxLength).IsRequired();

        // Optimistic concurrency between ingestion, detection and resolution, kept out of the domain model.
        builder.Property<byte[]>(RowVersion).IsRowVersion();

        builder.HasIndex(x => new { x.TenantId, x.FlowDefinitionId, x.BusinessKey }).IsUnique();
        builder.HasIndex(x => new { x.TenantId, x.EvaluationDueAt }).HasFilter("[EvaluationDueAt] IS NOT NULL");
        builder.HasIndex(x => new { x.TenantId, x.State });

        builder.HasOne<FlowDefinition>().WithMany().HasForeignKey(x => x.FlowDefinitionId);
        builder.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId);

        // One event can take part in several flows, so events and transactions are many-to-many.
        builder.HasMany(x => x.Events)
            .WithMany()
            .UsingEntity<Dictionary<string, object>>(
                "TransactionEvents",
                right => right.HasOne<IntegrationEvent>().WithMany().HasForeignKey("IntegrationEventId"),
                left => left.HasOne<Transaction>().WithMany().HasForeignKey("TransactionId"),
                join =>
                {
                    join.ToTable("TransactionEvents");
                    join.HasKey("TransactionId", "IntegrationEventId");
                    join.HasIndex("IntegrationEventId");
                });
        builder.Navigation(x => x.Events).HasField("_events").UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(x => x.Exceptions).WithOne().HasForeignKey(x => x.TransactionId);
        builder.Navigation(x => x.Exceptions).HasField("_exceptions").UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
