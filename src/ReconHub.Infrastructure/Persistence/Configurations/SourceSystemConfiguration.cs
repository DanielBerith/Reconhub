using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReconHub.Domain.SourceSystems;
using ReconHub.Domain.Tenants;

namespace ReconHub.Infrastructure.Persistence.Configurations;

internal sealed class SourceSystemConfiguration : IEntityTypeConfiguration<SourceSystem>
{
    public void Configure(EntityTypeBuilder<SourceSystem> builder)
    {
        builder.ToTable("SourceSystems");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).HasMaxLength(SourceSystem.NameMaxLength).IsRequired();
        builder.Property(x => x.ApiKeyHash)
            .HasMaxLength(SourceSystem.ApiKeyHashLength)
            .IsFixedLength()
            .IsUnicode(false)
            .IsRequired();

        builder.HasIndex(x => x.ApiKeyHash).IsUnique();
        builder.HasIndex(x => new { x.TenantId, x.Name }).IsUnique();

        builder.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId);
    }
}
