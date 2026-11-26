namespace ReconHub.Domain.Common;

/// <summary>Marks an entity that belongs to exactly one tenant. Persistence enforces isolation on this property.</summary>
public interface ITenantScoped
{
    Guid TenantId { get; }
}
