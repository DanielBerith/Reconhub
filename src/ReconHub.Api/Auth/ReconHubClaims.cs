using System.Security.Claims;

namespace ReconHub.Api.Auth;

public static class ReconHubClaims
{
    /// <summary>Tenant of the caller: set by the API key handler, or issued in operator JWTs.</summary>
    public const string TenantId = "tenant_id";

    /// <summary>Source system of an API key caller.</summary>
    public const string SourceSystemId = "source_system_id";

    public static Guid GetSourceSystemId(this ClaimsPrincipal user)
    {
        ArgumentNullException.ThrowIfNull(user);
        return Guid.TryParse(user.FindFirstValue(SourceSystemId), out var id)
            ? id
            : throw new InvalidOperationException("The caller is not an authenticated source system.");
    }
}

public static class AuthorizationPolicies
{
    /// <summary>A source system authenticated with its API key (X-Api-Key).</summary>
    public const string SourceSystem = nameof(SourceSystem);

    /// <summary>A support-team user authenticated with a JWT carrying a tenant_id claim.</summary>
    public const string Operator = nameof(Operator);
}
