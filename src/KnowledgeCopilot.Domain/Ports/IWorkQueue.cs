using System.Diagnostics.CodeAnalysis;
using KnowledgeCopilot.Contracts.Jobs;

namespace KnowledgeCopilot.Domain.Ports;

/// <summary>
/// At-least-once job queue with visibility timeouts (D-05). A received message is hidden until it is
/// completed or dead-lettered, or until the visibility timeout elapses and it is delivered again.
/// </summary>
[SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "Port name fixed by architecture §6; it is a queue.")]
public interface IWorkQueue
{
    Task EnqueueAsync(JobMessage message, CancellationToken cancellationToken);

    /// <summary>
    /// Receives up to <paramref name="maxMessages"/> visible messages and hides them for
    /// <paramref name="visibilityTimeout"/>. Each delivery increments <see cref="ReceivedJob.DequeueCount"/>.
    /// Returns an empty list when nothing is visible.
    /// </summary>
    Task<IReadOnlyList<ReceivedJob>> ReceiveAsync(
        int maxMessages,
        TimeSpan visibilityTimeout,
        CancellationToken cancellationToken);

    /// <summary>Removes the message. Idempotent for the same receipt.</summary>
    /// <exception cref="JobReceiptExpiredException">The message was delivered again under a newer receipt.</exception>
    Task CompleteAsync(JobReceipt receipt, CancellationToken cancellationToken);

    /// <summary>Moves the message out of the queue so it is never delivered again.</summary>
    /// <exception cref="JobReceiptExpiredException">The message was delivered again under a newer receipt.</exception>
    Task DeadLetterAsync(JobReceipt receipt, string reason, CancellationToken cancellationToken);
}

/// <summary>One delivery of a message.</summary>
/// <param name="Message">The job.</param>
/// <param name="Receipt">Proof of this delivery; needed to complete or dead-letter it.</param>
/// <param name="DequeueCount">How many times the message has been delivered, including this time (starts at 1).</param>
public sealed record ReceivedJob(JobMessage Message, JobReceipt Receipt, long DequeueCount);

/// <summary>Identifies one delivery of one message.</summary>
public sealed record JobReceipt(string MessageId, string PopReceipt);
