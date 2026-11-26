using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReconHub.Domain.Auditing;
using ReconHub.Domain.Exceptions;
using ReconHub.Domain.Tenants;

namespace ReconHub.Infrastructure.Persistence.Configurations;

internal sealed class AuditEntryConfiguration : IEntityTypeConfiguration<AuditEntry>
{
    public void Configure(EntityTypeBuilder<AuditEntry> builder)
    {
        builder.ToTable("AuditEntries");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Actor).HasMaxLength(AuditEntry.ActorMaxLength).IsRequired();
        builder.Property(x => x.Details).HasMaxLength(AuditEntry.DetailsMaxLength);

        builder.HasIndex(x => new { x.TenantId, x.ReconExceptionId, x.OccurredAt });

        builder.HasOne<ReconException>().WithMany().HasForeignKey(x => x.ReconExceptionId);
        builder.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId);
    }
}
