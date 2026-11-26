using System.Security.Claims;
using ReconHub.Application.Abstractions;

namespace ReconHub.Api.Auth;

internal sealed class ClaimsCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    // JWT claims are not remapped (MapInboundClaims = false), so look for the raw names first.
    private static readonly string[] NameClaimTypes =
        ["name", "preferred_username", "unique_name", "email", "sub", ClaimTypes.Name, ClaimTypes.NameIdentifier];

    public string Name
    {
        get
        {
            var user = httpContextAccessor.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated != true)
            {
                throw new InvalidOperationException("No authenticated user.");
            }

            foreach (var claimType in NameClaimTypes)
            {
                if (user.FindFirst(claimType)?.Value is { Length: > 0 } value)
                {
                    return value;
                }
            }

            throw new InvalidOperationException("The authenticated user has no name or subject claim.");
        }
    }
}
