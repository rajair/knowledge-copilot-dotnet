namespace KnowledgeCopilot.Contracts.Answers;

/// <summary>A chunk that was given to the model as context.</summary>
public sealed record RetrievedContext
{
    /// <summary>Deterministic chunk id.</summary>
    public required string ChunkId { get; init; }

    /// <summary>Owning document.</summary>
    public required Guid DocumentId { get; init; }

    /// <summary>Document title.</summary>
    public required string Title { get; init; }

    /// <summary>Chunk text as given to the model.</summary>
    public required string Text { get; init; }

    /// <summary>Final ranking score.</summary>
    public required double Score { get; init; }

    /// <summary>1-based page number, if the source is paged.</summary>
    public int? Page { get; init; }
}
