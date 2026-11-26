using ReconHub.Domain.Common;

namespace ReconHub.Domain.SourceSystems;

/// <summary>A registered system (WMS, ERP, carrier...) that reports events to ReconHub using its own API key.</summary>
public sealed class SourceSystem : Entity, ITenantScoped
{
    public const int NameMaxLength = 100;
    public const int ApiKeyHashLength = 64;

    public SourceSystem(Guid tenantId, string name, SourceSystemType type, string apiKeyHash, DateTimeOffset createdAt)
    {
        TenantId = Guard.NotEmpty(tenantId, nameof(tenantId));
        Name = Guard.Text(name, NameMaxLength, nameof(name));
        Type = type;
        ApiKeyHash = ValidateHash(apiKeyHash);
        CreatedAt = createdAt;
        IsActive = true;
    }

    private SourceSystem()
    {
        // EF Core
        Name = null!;
        ApiKeyHash = null!;
    }

    public Guid TenantId { get; private set; }

    public string Name { get; private set; }

    public SourceSystemType Type { get; private set; }

    /// <summary>Hex-encoded SHA-256 of the API key. The key itself is never stored.</summary>
    public string ApiKeyHash { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public void RotateApiKey(string newApiKeyHash) => ApiKeyHash = ValidateHash(newApiKeyHash);

    public void Deactivate() => IsActive = false;

    private static string ValidateHash(string apiKeyHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKeyHash);
        if (apiKeyHash.Length != ApiKeyHashLength)
        {
            throw new ArgumentException($"API key hash must be {ApiKeyHashLength} hex characters.", nameof(apiKeyHash));
        }

        return apiKeyHash;
    }
}
