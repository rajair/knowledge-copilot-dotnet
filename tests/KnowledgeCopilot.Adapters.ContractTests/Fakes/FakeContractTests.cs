using KnowledgeCopilot.Adapters.Fakes;
using KnowledgeCopilot.Domain.Ports;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

namespace KnowledgeCopilot.Adapters.ContractTests.Fakes;

public sealed class InMemoryIndexStoreContractTests : IndexStoreContract
{
    protected override Task<IIndexStore> CreateAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IIndexStore>(new InMemoryIndexStore());
}

public sealed class InMemoryWorkQueueContractTests : WorkQueueContract
{
    private readonly FakeTimeProvider _time = new();

    protected override Task<IWorkQueue> CreateAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IWorkQueue>(new InMemoryWorkQueue(_time));

    protected override Task AdvanceAsync(TimeSpan by)
    {
        _time.Advance(by);
        return Task.CompletedTask;
    }
}

public sealed class InMemoryBlobStoreContractTests : BlobStoreContract
{
    protected override Task<IBlobStore> CreateAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IBlobStore>(new InMemoryBlobStore());
}

public sealed class PassThroughRerankerContractTests : RerankerContract
{
    protected override Task<IReranker> CreateAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReranker>(new PassThroughReranker());
}

public sealed class DevCurrentPrincipalContractTests : CurrentPrincipalContract
{
    protected override ICurrentPrincipal Create()
    {
        var options = new DevPrincipalOptions();
        options.Groups.Add("hr");
        options.Groups.Add("eng");
        options.Groups.Add("hr");
        return new DevCurrentPrincipal(Options.Create(options));
    }
}
