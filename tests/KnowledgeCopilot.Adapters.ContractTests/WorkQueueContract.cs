using KnowledgeCopilot.Contracts.Jobs;
using KnowledgeCopilot.Domain.Ports;

namespace KnowledgeCopilot.Adapters.ContractTests;

/// <summary>
/// Behaviour every <see cref="IWorkQueue"/> must show (step 2 "Port semantics"). Time moves only through
/// <see cref="AdvanceAsync"/>, so the fake runs on a <c>FakeTimeProvider</c> and never sleeps.
/// </summary>
public abstract class WorkQueueContract
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    /// <summary>Visibility timeout used by every receive.</summary>
    protected virtual TimeSpan VisibilityTimeout => TimeSpan.FromSeconds(2);

    /// <summary>Creates an empty queue.</summary>
    protected abstract Task<IWorkQueue> CreateAsync(CancellationToken cancellationToken);

    /// <summary>Moves the queue's clock forward by <paramref name="by"/>.</summary>
    protected abstract Task AdvanceAsync(TimeSpan by);

    private TimeSpan PastTimeout => VisibilityTimeout + TimeSpan.FromSeconds(1);

    [Fact]
    public async Task Receive_EmptyQueue_ReturnsNothing()
    {
        var queue = await CreateAsync(Ct);

        Assert.Empty(await ReceiveAsync(queue));
    }

    [Fact]
    public async Task Receive_AfterEnqueue_ReturnsMessageWithDequeueCountOne()
    {
        var queue = await CreateAsync(Ct);
        var message = NewMessage();
        await queue.EnqueueAsync(message, Ct);

        var received = Assert.Single(await ReceiveAsync(queue));

        Assert.Equal(message, received.Message);
        Assert.Equal(1, received.DequeueCount);
    }

    [Fact]
    public async Task Receive_WithinVisibilityTimeout_MessageIsInvisible()
    {
        var queue = await CreateAsync(Ct);
        await queue.EnqueueAsync(NewMessage(), Ct);
        await ReceiveAsync(queue);

        await AdvanceAsync(VisibilityTimeout / 2);

        Assert.Empty(await ReceiveAsync(queue));
    }

    [Fact]
    public async Task Receive_AfterVisibilityTimeout_ReappearsWithIncrementedDequeueCount()
    {
        var queue = await CreateAsync(Ct);
        var message = NewMessage();
        await queue.EnqueueAsync(message, Ct);
        var first = Assert.Single(await ReceiveAsync(queue));

        await AdvanceAsync(PastTimeout);

        var second = Assert.Single(await ReceiveAsync(queue));
        Assert.Equal(message, second.Message);
        Assert.Equal(first.Receipt.MessageId, second.Receipt.MessageId);
        Assert.NotEqual(first.Receipt, second.Receipt);
        Assert.Equal(2, second.DequeueCount);
    }

    [Fact]
    public async Task Complete_ThenTimeoutElapses_NotRedelivered()
    {
        var queue = await CreateAsync(Ct);
        await queue.EnqueueAsync(NewMessage(), Ct);
        var received = Assert.Single(await ReceiveAsync(queue));

        await queue.CompleteAsync(received.Receipt, Ct);
        await AdvanceAsync(PastTimeout);

        Assert.Empty(await ReceiveAsync(queue));
    }

    [Fact]
    public async Task Complete_SameReceiptTwice_IsIdempotent()
    {
        var queue = await CreateAsync(Ct);
        await queue.EnqueueAsync(NewMessage(), Ct);
        var received = Assert.Single(await ReceiveAsync(queue));
        await queue.CompleteAsync(received.Receipt, Ct);

        await queue.CompleteAsync(received.Receipt, Ct);

        await AdvanceAsync(PastTimeout);
        Assert.Empty(await ReceiveAsync(queue));
    }

    [Fact]
    public async Task Complete_ExpiredReceipt_ThrowsJobReceiptExpiredException()
    {
        var queue = await CreateAsync(Ct);
        await queue.EnqueueAsync(NewMessage(), Ct);
        var stale = Assert.Single(await ReceiveAsync(queue));
        await AdvanceAsync(PastTimeout);
        var current = Assert.Single(await ReceiveAsync(queue));

        var exception = await Assert.ThrowsAsync<JobReceiptExpiredException>(() => queue.CompleteAsync(stale.Receipt, Ct));

        Assert.Equal(stale.Receipt.MessageId, exception.MessageId);
        await queue.CompleteAsync(current.Receipt, Ct);
    }

    [Fact]
    public async Task DeadLetter_Message_NotRedelivered()
    {
        var queue = await CreateAsync(Ct);
        await queue.EnqueueAsync(NewMessage(), Ct);
        var received = Assert.Single(await ReceiveAsync(queue));

        await queue.DeadLetterAsync(received.Receipt, "poison", Ct);
        await AdvanceAsync(PastTimeout);

        Assert.Empty(await ReceiveAsync(queue));
    }

    [Fact]
    public async Task Receive_MaxMessages_ReturnsAtMostThatManyAndKeepsTheRest()
    {
        var queue = await CreateAsync(Ct);
        for (var i = 0; i < 3; i++)
        {
            await queue.EnqueueAsync(NewMessage(), Ct);
        }

        var firstBatch = await queue.ReceiveAsync(2, VisibilityTimeout, Ct);
        var secondBatch = await queue.ReceiveAsync(2, VisibilityTimeout, Ct);

        Assert.Equal(2, firstBatch.Count);
        Assert.Single(secondBatch);
        Assert.Equal(3, firstBatch.Concat(secondBatch).Select(r => r.Message.JobId).Distinct().Count());
    }

    private Task<IReadOnlyList<ReceivedJob>> ReceiveAsync(IWorkQueue queue) =>
        queue.ReceiveAsync(maxMessages: 10, VisibilityTimeout, Ct);

    private static JobMessage NewMessage() => new()
    {
        JobId = Guid.NewGuid(),
        Kind = JobKind.Index,
        DocumentId = Guid.NewGuid(),
        TenantId = "t1",
    };
}
