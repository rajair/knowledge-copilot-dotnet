using KnowledgeCopilot.Domain.Indexing;
using KnowledgeCopilot.Domain.Ports;
using KnowledgeCopilot.Domain.Security;

namespace KnowledgeCopilot.Adapters.Fakes;

/// <summary>
/// In-process <see cref="IIndexStore"/>. The ACL filter is applied while selecting candidates, before any
/// scoring, exactly as a real engine pre-filters. Dense search is cosine similarity; sparse search counts
/// query-term occurrences. No native hybrid.
/// </summary>
public sealed class InMemoryIndexStore : IIndexStore
{
    private readonly Lock _gate = new();
    private readonly Dictionary<string, IndexedChunk> _chunks = new(StringComparer.Ordinal);

    public IndexCapabilities Capabilities { get; } = new(NativeHybrid: false, SparseVectors: false);

    public Task EnsureIndexAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task UpsertAsync(IReadOnlyList<IndexedChunk> chunks, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(chunks);
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            foreach (var chunk in chunks)
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(chunk.TenantId, nameof(chunks));
                _chunks[chunk.ChunkId] = chunk with
                {
                    AclPrincipals = [.. chunk.AclPrincipals],
                    Dense = chunk.Dense.ToArray(),
                };
            }
        }

        return Task.CompletedTask;
    }

    public Task DeleteByDocumentAsync(string tenantId, Guid documentId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            foreach (var chunk in ChunksOf(tenantId, documentId))
            {
                _chunks.Remove(chunk.ChunkId);
            }
        }

        return Task.CompletedTask;
    }

    public Task DeleteChunksAsync(string tenantId, IReadOnlyList<string> chunkIds, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(chunkIds);
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            foreach (var chunkId in chunkIds)
            {
                if (_chunks.TryGetValue(chunkId, out var chunk) && IsTenant(chunk, tenantId))
                {
                    _chunks.Remove(chunkId);
                }
            }
        }

        return Task.CompletedTask;
    }

    public Task UpdateAclAsync(
        string tenantId,
        Guid documentId,
        IReadOnlyList<string> aclPrincipals,
        int aclVersion,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(aclPrincipals);
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            foreach (var chunk in ChunksOf(tenantId, documentId))
            {
                _chunks[chunk.ChunkId] = chunk with { AclPrincipals = [.. aclPrincipals], AclVersion = aclVersion };
            }
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<ScoredChunk>> SearchDenseAsync(
        ReadOnlyMemory<float> vector,
        AclFilter filter,
        int k,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<IReadOnlyList<ScoredChunk>>(Search(filter, k, chunk => CosineSimilarity(vector.Span, chunk.Dense.Span)));
    }

    public Task<IReadOnlyList<ScoredChunk>> SearchSparseAsync(
        string text,
        AclFilter filter,
        int k,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var queryTerms = Tokenize(text).ToHashSet(StringComparer.Ordinal);
        return Task.FromResult<IReadOnlyList<ScoredChunk>>(Search(filter, k, chunk =>
        {
            var hits = Tokenize(chunk.Text).Count(queryTerms.Contains);
            return hits == 0 ? null : hits;
        }));
    }

    public Task<IReadOnlyList<ScoredChunk>> SearchHybridAsync(
        string text,
        ReadOnlyMemory<float> vector,
        AclFilter filter,
        int k,
        CancellationToken cancellationToken) =>
        throw new NotSupportedException("InMemoryIndexStore has no native hybrid search; fuse dense and sparse results instead.");

    private List<ScoredChunk> Search(AclFilter filter, int k, Func<IndexedChunk, double?> score)
    {
        ArgumentNullException.ThrowIfNull(filter);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(k);

        lock (_gate)
        {
            return _chunks.Values
                .Where(chunk => filter.Allows(chunk.TenantId, chunk.AclPrincipals))
                .Select(chunk => (Chunk: chunk, Score: score(chunk)))
                .Where(scored => scored.Score is not null)
                .OrderByDescending(scored => scored.Score)
                .ThenBy(scored => scored.Chunk.ChunkId, StringComparer.Ordinal)
                .Take(k)
                .Select(scored => ToScored(scored.Chunk, scored.Score!.Value))
                .ToList();
        }
    }

    private List<IndexedChunk> ChunksOf(string tenantId, Guid documentId) =>
        [.. _chunks.Values.Where(chunk => IsTenant(chunk, tenantId) && chunk.DocumentId == documentId)];

    private static bool IsTenant(IndexedChunk chunk, string tenantId) =>
        string.Equals(chunk.TenantId, tenantId, StringComparison.Ordinal);

    private static double? CosineSimilarity(ReadOnlySpan<float> a, ReadOnlySpan<float> b)
    {
        if (a.Length != b.Length)
        {
            throw new ArgumentException($"Vector has {a.Length} dimensions; the index has {b.Length}.");
        }

        double dot = 0, normA = 0, normB = 0;
        for (var i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            normA += a[i] * a[i];
            normB += b[i] * b[i];
        }

        return normA == 0 || normB == 0 ? 0 : dot / (Math.Sqrt(normA) * Math.Sqrt(normB));
    }

    private static IEnumerable<string> Tokenize(string text) =>
        text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
            .Select(token => new string([.. token.Where(char.IsLetterOrDigit)]).ToUpperInvariant())
            .Where(token => token.Length > 0);

    private static ScoredChunk ToScored(IndexedChunk chunk, double score) => new()
    {
        ChunkId = chunk.ChunkId,
        TenantId = chunk.TenantId,
        DocumentId = chunk.DocumentId,
        AclVersion = chunk.AclVersion,
        Title = chunk.Title,
        HeadingPath = chunk.HeadingPath,
        Page = chunk.Page,
        Text = chunk.Text,
        Score = score,
    };
}
