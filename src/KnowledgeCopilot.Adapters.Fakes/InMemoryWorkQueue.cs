using System.Diagnostics.CodeAnalysis;
using KnowledgeCopilot.Contracts.Jobs;
using KnowledgeCopilot.Domain.Ports;

namespace KnowledgeCopilot.Adapters.Fakes;

/// <summary>
/// In-process <see cref="IWorkQueue"/> with Azure Storage Queue semantics: a received message is hidden for
/// the visibility timeout (measured with the injected <see cref="TimeProvider"/>) and reappears with a new
/// receipt and a higher dequeue count if it is not completed. A receipt stays valid until the message is
/// delivered again.
/// </summary>
[SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "Implements IWorkQueue; it is a queue.")]
public sealed class InMemoryWorkQueue(TimeProvider timeProvider) : IWorkQueue
{
    private readonly Lock _gate = new();
    private readonly List<Entry> _messages = [];
    private readonly HashSet<JobReceipt> _finished = [];
    private readonly List<(JobMessage Message, string Reason)> _deadLetters = [];

    /// <summary>Messages moved out of the queue by <see cref="DeadLetterAsync"/>, for assertions.</summary>
    public IReadOnlyList<(JobMessage Message, string Reason)> DeadLetters
    {
        get
        {
            lock (_gate)
            {
                return [.. _deadLetters];
            }
        }
    }

    public Task EnqueueAsync(JobMessage message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            _messages.Add(new Entry(Guid.NewGuid().ToString("N"), message, timeProvider.GetUtcNow()));
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<ReceivedJob>> ReceiveAsync(
        int maxMessages,
        TimeSpan visibilityTimeout,
        CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxMessages);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(visibilityTimeout, TimeSpan.Zero);
        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)
        {
            var now = timeProvider.GetUtcNow();
            var received = new List<ReceivedJob>();
            foreach (var entry in _messages.Where(e => e.VisibleAt <= now).Take(maxMessages))
            {
                entry.PopReceipt = Guid.NewGuid().ToString("N");
                entry.VisibleAt = now + visibilityTimeout;
                entry.DequeueCount++;
                received.Add(new ReceivedJob(entry.Message, new JobReceipt(entry.Id, entry.PopReceipt), entry.DequeueCount));
            }

            return Task.FromResult<IReadOnlyList<ReceivedJob>>(received);
        }
    }

    public Task CompleteAsync(JobReceipt receipt, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            Remove(receipt);
        }

        return Task.CompletedTask;
    }

    public Task DeadLetterAsync(JobReceipt receipt, string reason, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(reason);
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            if (Remove(receipt) is { } message)
            {
                _deadLetters.Add((message, reason));
            }
        }

        return Task.CompletedTask;
    }

    /// <returns>The removed message, or null when this receipt already removed it.</returns>
    private JobMessage? Remove(JobReceipt receipt)
    {
        ArgumentNullException.ThrowIfNull(receipt);
        if (_finished.Contains(receipt))
        {
            return null;
        }

        var entry = _messages.Find(e => string.Equals(e.Id, receipt.MessageId, StringComparison.Ordinal));
        if (entry is null || !string.Equals(entry.PopReceipt, receipt.PopReceipt, StringComparison.Ordinal))
        {
            throw JobReceiptExpiredException.For(receipt);
        }

        _messages.Remove(entry);
        _finished.Add(receipt);
        return entry.Message;
    }

    private sealed class Entry(string id, JobMessage message, DateTimeOffset visibleAt)
    {
        public string Id { get; } = id;

        public JobMessage Message { get; } = message;

        public DateTimeOffset VisibleAt { get; set; } = visibleAt;

        public string? PopReceipt { get; set; }

        public long DequeueCount { get; set; }
    }
}
