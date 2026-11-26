using ReconHub.Application.Abstractions;

namespace ReconHub.Infrastructure.Tenancy;

/// <summary>
/// Scoped holder of the current tenant. Set once per scope: by the API from the authenticated principal,
/// or by the detection job for each tenant it processes.
/// </summary>
public sealed class TenantContext : ITenantContext
{
    public Guid? TenantId { get; private set; }

    public void SetTenant(Guid tenantId)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("Tenant id must not be empty.", nameof(tenantId));
        }

        if (TenantId is { } current && current != tenantId)
        {
            throw new InvalidOperationException("The tenant of a scope cannot change once set.");
        }

        TenantId = tenantId;
    }
}
