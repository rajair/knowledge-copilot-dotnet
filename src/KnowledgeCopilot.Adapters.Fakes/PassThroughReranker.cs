using KnowledgeCopilot.Domain.Indexing;
using KnowledgeCopilot.Domain.Ports;

namespace KnowledgeCopilot.Adapters.Fakes;

/// <summary>Keeps the retrieval order; the fallback when no reranker is configured (D-09).</summary>
public sealed class PassThroughReranker : IReranker
{
    public Task<IReadOnlyList<ScoredChunk>> RerankAsync(
        string query,
        IReadOnlyList<ScoredChunk> candidates,
        int topN,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        ArgumentOutOfRangeException.ThrowIfNegative(topN);
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyList<ScoredChunk> result = [.. candidates.DistinctBy(c => c.ChunkId, StringComparer.Ordinal).Take(topN)];
        return Task.FromResult(result);
    }
}
