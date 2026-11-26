namespace ReconHub.Application.Abstractions;

/// <summary>The authenticated human operator (support team member) making the request.</summary>
public interface ICurrentUser
{
    /// <summary>A display name suitable for audit records. Throws if no user is authenticated.</summary>
    string Name { get; }
}
