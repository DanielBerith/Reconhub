namespace ReconHub.Application.Abstractions;

/// <summary>The tenant the current operation runs for. Set from the authenticated caller, or by the detection job per tenant.</summary>
public interface ITenantContext
{
    Guid? TenantId { get; }
}

public static class TenantContextExtensions
{
    public static Guid RequireTenantId(this ITenantContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.TenantId ?? throw new InvalidOperationException("No tenant is set for this operation.");
    }
}
