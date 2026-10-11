namespace KnowledgeCopilot.Contracts;

/// <summary>
/// Semantic version of the wire contracts. A breaking change (removed or renamed member, changed type,
/// new required member) needs a major bump and a decision entry in docs/decisions.md.
/// </summary>
public static class ContractsVersion
{
    /// <summary>The current contracts version.</summary>
    public const string Value = "1.0.0";
}
