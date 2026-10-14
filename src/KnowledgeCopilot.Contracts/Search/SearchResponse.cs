namespace KnowledgeCopilot.Contracts.Search;

/// <summary>Ranked hits for a search request.</summary>
public sealed record SearchResponse
{
    /// <summary>Hits ordered by rank.</summary>
    public required IReadOnlyList<SearchHit> Hits { get; init; }

    /// <summary>Stage timings in milliseconds, keyed by stage name (for example <c>dense</c>, <c>rerank</c>).</summary>
    public required IReadOnlyDictionary<string, double> Timings { get; init; }
}
