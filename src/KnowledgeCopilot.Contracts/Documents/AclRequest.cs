namespace KnowledgeCopilot.Contracts.Documents;

/// <summary>Who may read a document. Principals are opaque ids; the server adds the tenant (D-14).</summary>
public sealed record AclRequest
{
    /// <summary>User ids that may read the document.</summary>
    public required IReadOnlyList<string> Users { get; init; }

    /// <summary>Group ids whose members may read the document.</summary>
    public required IReadOnlyList<string> Groups { get; init; }

    /// <summary>When true, every user in the caller's tenant may read the document.</summary>
    public required bool WholeTenant { get; init; }
}
