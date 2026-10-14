namespace KnowledgeCopilot.Domain.Indexing;

/// <summary>A search or rerank result. Carries no vectors.</summary>
public sealed record ScoredChunk
{
    public required string ChunkId { get; init; }

    public required string TenantId { get; init; }

    public required Guid DocumentId { get; init; }

    public required int AclVersion { get; init; }

    public required string Title { get; init; }

    public string? HeadingPath { get; init; }

    public int? Page { get; init; }

    public required string Text { get; init; }

    /// <summary>Higher is better. Only comparable within one result list.</summary>
    public required double Score { get; init; }
}
