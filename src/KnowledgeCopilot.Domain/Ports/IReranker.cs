using KnowledgeCopilot.Domain.Indexing;

namespace KnowledgeCopilot.Domain.Ports;

/// <summary>Reorders retrieved candidates by relevance to the query (D-09).</summary>
public interface IReranker
{
    /// <summary>
    /// Returns a subset of <paramref name="candidates"/>, at most <paramref name="topN"/>, each chunk id at
    /// most once, best first. Never adds chunks that were not in the input.
    /// </summary>
    Task<IReadOnlyList<ScoredChunk>> RerankAsync(
        string query,
        IReadOnlyList<ScoredChunk> candidates,
        int topN,
        CancellationToken cancellationToken);
}
