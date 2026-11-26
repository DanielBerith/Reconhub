using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReconHub.Domain.Events;
using ReconHub.Domain.SourceSystems;
using ReconHub.Domain.Tenants;

namespace ReconHub.Infrastructure.Persistence.Configurations;

internal sealed class IntegrationEventConfiguration : IEntityTypeConfiguration<IntegrationEvent>
{
    public void Configure(EntityTypeBuilder<IntegrationEvent> builder)
    {
        builder.ToTable("IntegrationEvents", table =>
            table.HasCheckConstraint("CK_IntegrationEvents_Payload_IsJson", "ISJSON([Payload]) = 1"));
        builder.HasKey(x => x.Id);

        builder.Property(x => x.EventType).HasMaxLength(IntegrationEvent.EventTypeMaxLength).IsRequired();
        builder.Property(x => x.BusinessKey).HasMaxLength(IntegrationEvent.BusinessKeyMaxLength).IsRequired();
        builder.Property(x => x.IdempotencyKey).HasMaxLength(IntegrationEvent.IdempotencyKeyMaxLength).IsRequired();
        builder.Property(x => x.Payload).IsRequired();

        // Idempotency is scoped to the reporting system.
        builder.HasIndex(x => new { x.SourceSystemId, x.IdempotencyKey }).IsUnique();
        builder.HasIndex(x => new { x.TenantId, x.BusinessKey });

        builder.HasOne<SourceSystem>().WithMany().HasForeignKey(x => x.SourceSystemId);
        builder.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId);
    }
}
