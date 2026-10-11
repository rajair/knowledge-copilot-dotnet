namespace KnowledgeCopilot.Contracts.Answers;

/// <summary>A source the answer cites. Always one of the retrieved chunks.</summary>
public sealed record Citation
{
    /// <summary>Cited chunk id.</summary>
    public required string ChunkId { get; init; }

    /// <summary>Owning document.</summary>
    public required Guid DocumentId { get; init; }

    /// <summary>Document title.</summary>
    public required string Title { get; init; }

    /// <summary>Short excerpt shown in the citation chip.</summary>
    public required string Snippet { get; init; }

    /// <summary>1-based page number, if the source is paged.</summary>
    public int? Page { get; init; }
}
