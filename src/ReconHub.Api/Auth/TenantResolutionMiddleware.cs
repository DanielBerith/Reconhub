using ReconHub.Infrastructure.Tenancy;
using Serilog;
using Serilog.Context;

namespace ReconHub.Api.Auth;

/// <summary>
/// Sets the request's tenant from the authenticated principal. Runs after UseAuthorization, which is where
/// endpoint-specific schemes (API key or JWT) authenticate. Requests without a tenant see no tenant data.
/// </summary>
internal sealed class TenantResolutionMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, TenantContext tenantContext, IDiagnosticContext diagnosticContext)
    {
        if (!Guid.TryParse(context.User.FindFirst(ReconHubClaims.TenantId)?.Value, out var tenantId))
        {
            await next(context);
            return;
        }

        tenantContext.SetTenant(tenantId);
        diagnosticContext.Set("TenantId", tenantId);
        using (LogContext.PushProperty("TenantId", tenantId))
        {
            await next(context);
        }
    }
}
