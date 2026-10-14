using KnowledgeCopilot.Domain.Indexing;
using KnowledgeCopilot.Domain.Ports;
using KnowledgeCopilot.Domain.Security;

namespace KnowledgeCopilot.Adapters.ContractTests;

/// <summary>
/// Behaviour every <see cref="IIndexStore"/> must show (step 2 "Port semantics"). Each test uses fresh
/// tenant ids, so a subclass may share one physical index across tests.
/// </summary>
public abstract class IndexStoreContract
{
    protected const int Dimensions = 4;

    private readonly string _tenant = NewTenant();
    private readonly string _otherTenant = NewTenant();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    /// <summary>Creates a store whose index has <see cref="Dimensions"/> dense dimensions.</summary>
    protected abstract Task<IIndexStore> CreateAsync(CancellationToken cancellationToken);

    [Fact]
    public async Task Upsert_SameChunkIdTwice_ReplacesPayloadAndVector()
    {
        var store = await CreateStoreAsync();
        await store.UpsertAsync([Chunk("c1", [1, 0, 0, 0], text: "old")], Ct);

        await store.UpsertAsync([Chunk("c1", [0, 1, 0, 0], text: "new")], Ct);

        var alongNew = Assert.Single(await store.SearchDenseAsync(Vector(0, 1, 0, 0), Filter(), 10, Ct));
        var alongOld = Assert.Single(await store.SearchDenseAsync(Vector(1, 0, 0, 0), Filter(), 10, Ct));
        Assert.Equal("new", alongNew.Text);
        Assert.True(alongNew.Score > alongOld.Score, "The replaced chunk must be scored with the new vector.");
    }

    [Fact]
    public async Task Search_OtherTenant_ReturnsNothing()
    {
        var store = await CreateStoreAsync();
        await store.UpsertAsync([Chunk("c1", [1, 0, 0, 0], tenant: _otherTenant, acl: ["g:hr", "u:alice"])], Ct);

        var dense = await store.SearchDenseAsync(Vector(1, 0, 0, 0), Filter(groups: ["hr"]), 10, Ct);
        var sparse = await SearchSparseAsync(store, "policy", Filter(groups: ["hr"]));

        Assert.Empty(dense);
        Assert.Empty(sparse);
    }

    [Fact]
    public async Task Search_NoPrincipalOverlap_ReturnsNothing()
    {
        var store = await CreateStoreAsync();
        await store.UpsertAsync([Chunk("c1", [1, 0, 0, 0], acl: ["g:finance", "u:bob"])], Ct);

        var dense = await store.SearchDenseAsync(Vector(1, 0, 0, 0), Filter(groups: ["hr"]), 10, Ct);
        var sparse = await SearchSparseAsync(store, "policy", Filter(groups: ["hr"]));

        Assert.Empty(dense);
        Assert.Empty(sparse);
    }

    [Theory]
    [InlineData("g:hr")]
    [InlineData("u:alice")]
    public async Task Search_SharedPrincipal_ReturnsChunk(string principal)
    {
        var store = await CreateStoreAsync();
        await store.UpsertAsync([Chunk("c1", [1, 0, 0, 0], acl: [principal])], Ct);

        var results = await store.SearchDenseAsync(Vector(1, 0, 0, 0), Filter(groups: ["hr"]), 10, Ct);

        Assert.Equal("c1", Assert.Single(results).ChunkId);
    }

    [Fact]
    public async Task Search_TenantWidePrincipal_ReturnsChunk()
    {
        var store = await CreateStoreAsync();
        await store.UpsertAsync([Chunk("c1", [1, 0, 0, 0], acl: [AclPrincipal.Tenant(_tenant)])], Ct);

        var results = await store.SearchDenseAsync(Vector(1, 0, 0, 0), Filter(), 10, Ct);

        Assert.Single(results);
    }

    [Fact]
    public async Task Search_MixedVisibility_ReturnsOnlyAllowedChunks()
    {
        var store = await CreateStoreAsync();
        await store.UpsertAsync(
        [
            Chunk("allowed", [1, 0, 0, 0], acl: ["g:hr"]),
            Chunk("denied", [1, 0, 0, 0], acl: ["g:finance"]),
            Chunk("foreign", [1, 0, 0, 0], tenant: _otherTenant, acl: ["g:hr"]),
        ], Ct);

        var results = await store.SearchDenseAsync(Vector(1, 0, 0, 0), Filter(groups: ["hr"]), 10, Ct);

        Assert.Equal(["allowed"], results.Select(r => r.ChunkId));
    }

    [Fact]
    public async Task SearchDense_SeveralMatches_SortedByScoreDescending()
    {
        var store = await CreateStoreAsync();
        await store.UpsertAsync(
        [
            Chunk("far", [0, 0, 0, 1]),
            Chunk("near", [1, 0.1f, 0, 0]),
            Chunk("mid", [1, 1, 0, 0]),
        ], Ct);

        var results = await store.SearchDenseAsync(Vector(1, 0, 0, 0), Filter(), 10, Ct);

        Assert.Equal(["near", "mid", "far"], results.Select(r => r.ChunkId));
        Assert.Equal(results.Select(r => r.Score).OrderDescending(), results.Select(r => r.Score));
    }

    [Fact]
    public async Task SearchDense_EqualScores_TiesBrokenByChunkIdOrdinal()
    {
        var store = await CreateStoreAsync();
        await store.UpsertAsync(
        [
            Chunk("b", [1, 0, 0, 0]),
            Chunk("B", [1, 0, 0, 0]),
            Chunk("a", [1, 0, 0, 0]),
        ], Ct);

        var results = await store.SearchDenseAsync(Vector(1, 0, 0, 0), Filter(), 10, Ct);

        Assert.Equal(["B", "a", "b"], results.Select(r => r.ChunkId));
    }

    [Fact]
    public async Task SearchDense_MoreMatchesThanK_ReturnsTopK()
    {
        var store = await CreateStoreAsync();
        await store.UpsertAsync([.. Enumerable.Range(0, 5).Select(i => Chunk($"c{i}", [1, i, 0, 0]))], Ct);

        var results = await store.SearchDenseAsync(Vector(1, 0, 0, 0), Filter(), 2, Ct);

        Assert.Equal(["c0", "c1"], results.Select(r => r.ChunkId));
    }

    [Fact]
    public async Task SearchDense_Result_CarriesChunkPayload()
    {
        var store = await CreateStoreAsync();
        var documentId = Guid.NewGuid();
        await store.UpsertAsync([Chunk("c1", [1, 0, 0, 0], documentId: documentId, aclVersion: 3) with { HeadingPath = "A > B", Page = 7 }], Ct);

        var result = Assert.Single(await store.SearchDenseAsync(Vector(1, 0, 0, 0), Filter(), 10, Ct));

        Assert.Equal(_tenant, result.TenantId);
        Assert.Equal(documentId, result.DocumentId);
        Assert.Equal(3, result.AclVersion);
        Assert.Equal("Title c1", result.Title);
        Assert.Equal("A > B", result.HeadingPath);
        Assert.Equal(7, result.Page);
        Assert.Equal("policy text c1", result.Text);
    }

    [Fact]
    public async Task UpdateAcl_RevokedGroup_NoLongerReturned()
    {
        var store = await CreateStoreAsync();
        var documentId = Guid.NewGuid();
        await store.UpsertAsync(
        [
            Chunk("c1", [1, 0, 0, 0], documentId: documentId, acl: ["g:hr"]),
            Chunk("c2", [0, 1, 0, 0], documentId: documentId, acl: ["g:hr"]),
        ], Ct);

        await store.UpdateAclAsync(_tenant, documentId, ["g:finance"], aclVersion: 2, Ct);

        Assert.Empty(await store.SearchDenseAsync(Vector(1, 0, 0, 0), Filter(groups: ["hr"]), 10, Ct));
        Assert.Empty(await SearchSparseAsync(store, "policy", Filter(groups: ["hr"])));
    }

    [Fact]
    public async Task UpdateAcl_Document_ChangesPrincipalsAndVersionButNotVectors()
    {
        var store = await CreateStoreAsync();
        var documentId = Guid.NewGuid();
        await store.UpsertAsync(
        [
            Chunk("c1", [1, 0, 0, 0], documentId: documentId, acl: ["g:hr"]),
            Chunk("c2", [1, 1, 0, 0], documentId: documentId, acl: ["g:hr"]),
        ], Ct);
        var before = await store.SearchDenseAsync(Vector(1, 0, 0, 0), Filter(groups: ["hr"]), 10, Ct);

        await store.UpdateAclAsync(_tenant, documentId, ["g:finance"], aclVersion: 2, Ct);

        var after = await store.SearchDenseAsync(Vector(1, 0, 0, 0), Filter(groups: ["finance"]), 10, Ct);
        Assert.Equal(before.Select(r => (r.ChunkId, r.Score)), after.Select(r => (r.ChunkId, r.Score)));
        Assert.All(after, r => Assert.Equal(2, r.AclVersion));
    }

    [Fact]
    public async Task UpdateAcl_OtherDocument_Untouched()
    {
        var store = await CreateStoreAsync();
        var documentId = Guid.NewGuid();
        await store.UpsertAsync(
        [
            Chunk("target", [1, 0, 0, 0], documentId: documentId, acl: ["g:hr"]),
            Chunk("other", [1, 0, 0, 0], acl: ["g:hr"]),
        ], Ct);

        await store.UpdateAclAsync(_tenant, documentId, ["g:finance"], aclVersion: 2, Ct);

        var results = await store.SearchDenseAsync(Vector(1, 0, 0, 0), Filter(groups: ["hr"]), 10, Ct);
        Assert.Equal(["other"], results.Select(r => r.ChunkId));
        Assert.Equal(1, results[0].AclVersion);
    }

    [Fact]
    public async Task DeleteByDocument_Document_RemovesEveryChunk()
    {
        var store = await CreateStoreAsync();
        var documentId = Guid.NewGuid();
        await store.UpsertAsync(
        [
            Chunk("c1", [1, 0, 0, 0], documentId: documentId),
            Chunk("c2", [1, 1, 0, 0], documentId: documentId),
            Chunk("keep", [1, 0, 1, 0]),
        ], Ct);

        await store.DeleteByDocumentAsync(_tenant, documentId, Ct);

        var results = await store.SearchDenseAsync(Vector(1, 0, 0, 0), Filter(), 10, Ct);
        Assert.Equal(["keep"], results.Select(r => r.ChunkId));
    }

    [Fact]
    public async Task DeleteByDocument_OnlyAffectsThatTenant()
    {
        var store = await CreateStoreAsync();
        var documentId = Guid.NewGuid();
        await store.UpsertAsync(
        [
            Chunk("mine", [1, 0, 0, 0], documentId: documentId),
            Chunk("theirs", [1, 0, 0, 0], tenant: _otherTenant, documentId: documentId),
        ], Ct);

        await store.DeleteByDocumentAsync(_tenant, documentId, Ct);

        Assert.Empty(await store.SearchDenseAsync(Vector(1, 0, 0, 0), Filter(), 10, Ct));
        var theirs = await store.SearchDenseAsync(Vector(1, 0, 0, 0), Filter(tenant: _otherTenant), 10, Ct);
        Assert.Equal(["theirs"], theirs.Select(r => r.ChunkId));
    }

    [Fact]
    public async Task DeleteChunks_ListedIds_RemovesOnlyThose()
    {
        var store = await CreateStoreAsync();
        await store.UpsertAsync(
        [
            Chunk("c1", [1, 0, 0, 0]),
            Chunk("c2", [1, 0, 0, 0]),
            Chunk("c3", [1, 0, 0, 0]),
        ], Ct);

        await store.DeleteChunksAsync(_tenant, ["c1", "c3", "missing"], Ct);

        var results = await store.SearchDenseAsync(Vector(1, 0, 0, 0), Filter(), 10, Ct);
        Assert.Equal(["c2"], results.Select(r => r.ChunkId));
    }

    [Fact]
    public async Task SearchSparse_MatchingTerms_ReturnsOnlyMatchingChunks()
    {
        var store = await CreateStoreAsync();
        await store.UpsertAsync(
        [
            Chunk("leave", [1, 0, 0, 0], text: "parental leave policy"),
            Chunk("travel", [1, 0, 0, 0], text: "travel expenses"),
        ], Ct);

        var results = await SearchSparseAsync(store, "leave", Filter());

        Assert.Equal(["leave"], results.Select(r => r.ChunkId));
    }

    [Fact]
    public async Task SearchHybrid_WithoutNativeHybrid_ThrowsNotSupported()
    {
        var store = await CreateStoreAsync();
        Assert.SkipWhen(store.Capabilities.NativeHybrid, "The store supports native hybrid search.");

        await Assert.ThrowsAsync<NotSupportedException>(
            () => store.SearchHybridAsync("policy", Vector(1, 0, 0, 0), Filter(), 10, Ct));
    }

    private async Task<IIndexStore> CreateStoreAsync()
    {
        var store = await CreateAsync(Ct);
        await store.EnsureIndexAsync(Ct);
        return store;
    }

    private static Task<IReadOnlyList<ScoredChunk>> SearchSparseAsync(IIndexStore store, string text, AclFilter filter) =>
        store.SearchSparseAsync(text, filter, 10, Ct);

    private AclFilter Filter(string? tenant = null, IReadOnlyList<string>? groups = null) =>
        AclFilter.For(new Principal(tenant ?? _tenant, "alice", groups ?? [], []));

    private IndexedChunk Chunk(
        string chunkId,
        float[] dense,
        string? tenant = null,
        Guid? documentId = null,
        IReadOnlyList<string>? acl = null,
        int aclVersion = 1,
        string? text = null) => new()
        {
            ChunkId = chunkId,
            TenantId = tenant ?? _tenant,
            DocumentId = documentId ?? Guid.NewGuid(),
            AclPrincipals = acl ?? ["u:alice"],
            AclVersion = aclVersion,
            Title = $"Title {chunkId}",
            Text = text ?? $"policy text {chunkId}",
            ContentSha256 = new string('0', 64),
            Dense = dense,
        };

    private static ReadOnlyMemory<float> Vector(params float[] values) => values;

    private static string NewTenant() => $"t-{Guid.NewGuid():N}";
}
