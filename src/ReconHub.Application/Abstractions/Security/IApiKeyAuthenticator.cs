namespace ReconHub.Application.Abstractions.Security;

/// <summary>Resolves a source system from the API key it presented.</summary>
public interface IApiKeyAuthenticator
{
    /// <returns>The identity of the active source system owning the key, or null if the key is unknown or inactive.</returns>
    Task<SourceSystemIdentity?> AuthenticateAsync(string apiKey, CancellationToken cancellationToken);
}

public sealed record SourceSystemIdentity(Guid TenantId, Guid SourceSystemId, string Name);
