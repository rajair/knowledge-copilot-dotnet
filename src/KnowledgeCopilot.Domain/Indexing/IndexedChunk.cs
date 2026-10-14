namespace KnowledgeCopilot.Domain.Indexing;

/// <summary>One chunk as written to the search index (architecture §8).</summary>
public sealed record IndexedChunk
{
    /// <summary>Deterministic key; upserts with the same id replace the chunk.</summary>
    public required string ChunkId { get; init; }

    public required string TenantId { get; init; }

    public required Guid DocumentId { get; init; }

    /// <summary><c>u:…</c>, <c>g:…</c> and <c>t:…</c> principals allowed to read the chunk.</summary>
    public required IReadOnlyList<string> AclPrincipals { get; init; }

    public required int AclVersion { get; init; }

    public required string Title { get; init; }

    /// <summary>Headings above the chunk, joined for display (for example <c>Benefits &gt; Leave</c>).</summary>
    public string? HeadingPath { get; init; }

    public int? Page { get; init; }

    public required string Text { get; init; }

    public required string ContentSha256 { get; init; }

    /// <summary>Dense embedding; its length must match the index dimensions.</summary>
    public required ReadOnlyMemory<float> Dense { get; init; }
}
