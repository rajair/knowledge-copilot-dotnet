using KnowledgeCopilot.Adapters.Fakes;
using KnowledgeCopilot.Domain.Indexing;
using KnowledgeCopilot.Domain.Security;

namespace KnowledgeCopilot.Adapters.ContractTests.Fakes;

public sealed class InMemoryIndexStoreTests
{
    [Fact]
    public async Task SearchDense_WrongDimensions_ThrowsArgumentException()
    {
        var store = new InMemoryIndexStore();
        await store.UpsertAsync(
        [
            new IndexedChunk
            {
                ChunkId = "c1",
                TenantId = "t1",
                DocumentId = Guid.NewGuid(),
                AclPrincipals = ["u:alice"],
                AclVersion = 1,
                Title = "Title",
                Text = "Text",
                ContentSha256 = new string('0', 64),
                Dense = new float[] { 1, 0, 0, 0 },
            },
        ], TestContext.Current.CancellationToken);
        var filter = AclFilter.For(new Principal("t1", "alice", [], []));

        await Assert.ThrowsAsync<ArgumentException>(
            () => store.SearchDenseAsync(new float[] { 1, 0 }, filter, 10, TestContext.Current.CancellationToken));
    }

    [Fact]
    public void Capabilities_Always_NoNativeHybridNoSparseVectors()
    {
        Assert.Equal(new IndexCapabilities(NativeHybrid: false, SparseVectors: false), new InMemoryIndexStore().Capabilities);
    }
}
