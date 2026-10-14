using KnowledgeCopilot.Domain.Ports;

namespace KnowledgeCopilot.Adapters.ContractTests;

/// <summary>Behaviour every <see cref="IBlobStore"/> must show (step 2 "Port semantics").</summary>
public abstract class BlobStoreContract
{
    private static readonly Dictionary<string, string> NoMetadata = [];

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    protected abstract Task<IBlobStore> CreateAsync(CancellationToken cancellationToken);

    [Fact]
    public async Task OpenRead_AfterPut_ReturnsIdenticalBytes()
    {
        var store = await CreateAsync(Ct);
        var key = NewKey();
        var bytes = Enumerable.Range(0, 4096).Select(i => (byte)(i * 31)).ToArray();

        await store.PutAsync(key, new MemoryStream(bytes), new Dictionary<string, string> { ["contentType"] = "application/pdf" }, Ct);

        Assert.Equal(bytes, await ReadAllAsync(store, key));
    }

    [Fact]
    public async Task OpenRead_MissingKey_ThrowsBlobNotFoundException()
    {
        var store = await CreateAsync(Ct);
        var key = NewKey();

        var exception = await Assert.ThrowsAsync<BlobNotFoundException>(() => store.OpenReadAsync(key, Ct));

        Assert.Equal(key, exception.Key);
    }

    [Fact]
    public async Task Put_ExistingKey_Overwrites()
    {
        var store = await CreateAsync(Ct);
        var key = NewKey();
        await store.PutAsync(key, new MemoryStream([1, 2, 3]), NoMetadata, Ct);

        await store.PutAsync(key, new MemoryStream([9]), NoMetadata, Ct);

        Assert.Equal([9], await ReadAllAsync(store, key));
    }

    [Fact]
    public async Task OpenRead_AfterDelete_ThrowsBlobNotFoundException()
    {
        var store = await CreateAsync(Ct);
        var key = NewKey();
        await store.PutAsync(key, new MemoryStream([1]), NoMetadata, Ct);

        await store.DeleteAsync(key, Ct);

        await Assert.ThrowsAsync<BlobNotFoundException>(() => store.OpenReadAsync(key, Ct));
    }

    [Fact]
    public async Task Delete_MissingKey_DoesNotThrow()
    {
        var store = await CreateAsync(Ct);

        await store.DeleteAsync(NewKey(), Ct);
    }

    private static async Task<byte[]> ReadAllAsync(IBlobStore store, string key)
    {
        await using var stream = await store.OpenReadAsync(key, Ct);
        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, Ct);
        return buffer.ToArray();
    }

    private static string NewKey() => $"contract-tests/{Guid.NewGuid():N}/original";
}
