using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReconHub.Domain.Events;
using ReconHub.Domain.Exceptions;
using ReconHub.Domain.Tenants;

namespace ReconHub.Infrastructure.Persistence.Configurations;

internal sealed class ReconExceptionConfiguration : IEntityTypeConfiguration<ReconException>
{
    public void Configure(EntityTypeBuilder<ReconException> builder)
    {
        builder.ToTable("ReconExceptions");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Detail).HasMaxLength(ReconException.DetailMaxLength).IsRequired();
        builder.Property(x => x.ResolutionNote).HasMaxLength(ReconException.ResolutionNoteMaxLength);
        builder.Property(x => x.ResolvedBy).HasMaxLength(ReconException.ResolvedByMaxLength);

        // Backstop for "never raise the same exception twice", even if two detection runs overlap.
        builder.HasIndex(x => new { x.TransactionId, x.Type, x.IntegrationEventId }).IsUnique();
        builder.HasIndex(x => new { x.TenantId, x.Status, x.DetectedAt });
        builder.HasIndex(x => new { x.TenantId, x.Type, x.DetectedAt });

        builder.HasOne<IntegrationEvent>().WithMany().HasForeignKey(x => x.IntegrationEventId);
        builder.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId);
    }
}
