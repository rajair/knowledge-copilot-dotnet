using KnowledgeCopilot.Domain.Indexing;
using KnowledgeCopilot.Domain.Ports;

namespace KnowledgeCopilot.Adapters.ContractTests;

/// <summary>Behaviour every <see cref="IReranker"/> must show (step 2 "Port semantics").</summary>
public abstract class RerankerContract
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    protected abstract Task<IReranker> CreateAsync(CancellationToken cancellationToken);

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(5)]
    [InlineData(10)]
    public async Task Rerank_AnyTopN_ReturnsSubsetOfAtMostTopN(int topN)
    {
        var reranker = await CreateAsync(Ct);
        var candidates = Candidates("a", "b", "c", "d", "e");

        var result = await reranker.RerankAsync("parental leave", candidates, topN, Ct);

        Assert.True(result.Count <= topN, $"Expected at most {topN} results, got {result.Count}.");
        Assert.All(result, chunk => Assert.Contains(chunk.ChunkId, candidates.Select(c => c.ChunkId)));
    }

    [Fact]
    public async Task Rerank_DuplicateCandidates_ReturnsEachChunkAtMostOnce()
    {
        var reranker = await CreateAsync(Ct);

        var result = await reranker.RerankAsync("parental leave", Candidates("a", "b", "a", "c", "b"), 10, Ct);

        Assert.Equal(result.Count, result.Select(c => c.ChunkId).Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public async Task Rerank_NoCandidates_ReturnsEmpty()
    {
        var reranker = await CreateAsync(Ct);

        Assert.Empty(await reranker.RerankAsync("parental leave", [], 5, Ct));
    }

    private static List<ScoredChunk> Candidates(params string[] chunkIds) =>
    [
        .. chunkIds.Select((id, i) => new ScoredChunk
        {
            ChunkId = id,
            TenantId = "t1",
            DocumentId = Guid.Empty,
            AclVersion = 1,
            Title = $"Title {id}",
            Text = $"Text about parental leave {id}",
            Score = 1.0 / (i + 1),
        }),
    ];
}
