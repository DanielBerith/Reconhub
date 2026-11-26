using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using ReconHub.Application.Abstractions.Security;
using ReconHub.Infrastructure.Persistence;

namespace ReconHub.Infrastructure.Security;

public static class ApiKeyHasher
{
    public const int MaxApiKeyLength = 256;

    /// <summary>
    /// SHA-256, hex encoded. Generated keys carry 256 bits of entropy, so a fast unsalted hash is appropriate
    /// (as for GitHub tokens) and allows an indexed lookup by hash.
    /// </summary>
    public static string Hash(string apiKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(apiKey)));
    }

    /// <summary>Creates a new random API key, e.g. for provisioning a source system.</summary>
    public static string Generate() => "rh_" + Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(32));
}

internal sealed class ApiKeyAuthenticator(ReconHubDbContext db) : IApiKeyAuthenticator
{
    public async Task<SourceSystemIdentity?> AuthenticateAsync(string apiKey, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(apiKey) || apiKey.Length > ApiKeyHasher.MaxApiKeyLength)
        {
            return null;
        }

        var hash = ApiKeyHasher.Hash(apiKey);

        // The caller's tenant is what we are trying to find out, so this one lookup bypasses the tenant filter.
        return await (
                from system in db.SourceSystems.IgnoreQueryFilters([ReconHubDbContext.TenantFilter])
                join tenant in db.Tenants on system.TenantId equals tenant.Id
                where system.ApiKeyHash == hash && system.IsActive && tenant.IsActive
                select new SourceSystemIdentity(system.TenantId, system.Id, system.Name))
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);
    }
}
