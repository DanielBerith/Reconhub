using ReconHub.Domain.Common;

namespace ReconHub.Domain.Tenants;

/// <summary>An isolated customer of ReconHub. Every other record belongs to one tenant.</summary>
public sealed class Tenant : Entity
{
    public const int NameMaxLength = 200;

    public Tenant(Guid id, string name, DateTimeOffset createdAt)
    {
        Id = Guard.NotEmpty(id, nameof(id));
        Name = Guard.Text(name, NameMaxLength, nameof(name));
        CreatedAt = createdAt;
        IsActive = true;
    }

    private Tenant() => Name = null!; // EF Core

    public string Name { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
}
