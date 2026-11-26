namespace ReconHub.Domain.Common;

/// <summary>Base type for entities identified by a GUID.</summary>
public abstract class Entity
{
    public Guid Id { get; protected init; } = Guid.CreateVersion7();
}
