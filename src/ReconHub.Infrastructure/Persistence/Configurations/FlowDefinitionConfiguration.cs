using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReconHub.Domain.Events;
using ReconHub.Domain.Flows;
using ReconHub.Domain.SourceSystems;
using ReconHub.Domain.Tenants;

namespace ReconHub.Infrastructure.Persistence.Configurations;

internal sealed class FlowDefinitionConfiguration : IEntityTypeConfiguration<FlowDefinition>
{
    public void Configure(EntityTypeBuilder<FlowDefinition> builder)
    {
        builder.ToTable("FlowDefinitions");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).HasMaxLength(FlowDefinition.NameMaxLength).IsRequired();
        builder.Property(x => x.TriggerEventType).HasMaxLength(IntegrationEvent.EventTypeMaxLength).IsRequired();
        builder.Property(x => x.FollowUpEventType).HasMaxLength(IntegrationEvent.EventTypeMaxLength).IsRequired();

        // SQL Server's time type tops out at 24h; store whole seconds instead.
        builder.Property(x => x.CompletionWindow)
            .HasConversion(v => (long)v.TotalSeconds, v => TimeSpan.FromSeconds(v))
            .HasColumnName("CompletionWindowSeconds");

        // Stored as a JSON array, e.g. ["quantity"].
        builder.PrimitiveCollection(x => x.MatchFields)
            .HasField("_matchFields")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .IsRequired();

        builder.Ignore(x => x.Trigger);
        builder.Ignore(x => x.FollowUp);

        builder.HasIndex(x => new { x.TenantId, x.Name }).IsUnique();
        builder.HasIndex(x => new { x.TenantId, x.TriggerSourceSystemId, x.TriggerEventType });
        builder.HasIndex(x => new { x.TenantId, x.FollowUpSourceSystemId, x.FollowUpEventType });

        builder.HasOne<SourceSystem>().WithMany().HasForeignKey(x => x.TriggerSourceSystemId);
        builder.HasOne<SourceSystem>().WithMany().HasForeignKey(x => x.FollowUpSourceSystemId);
        builder.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId);
    }
}
