namespace KnowledgeCopilot.Domain.Ports;

/// <summary>
/// Thrown when a receipt no longer owns its message: the visibility timeout elapsed and the message was
/// delivered again, so another consumer may be processing it.
/// </summary>
public sealed class JobReceiptExpiredException : Exception
{
    public JobReceiptExpiredException()
    {
    }

    public JobReceiptExpiredException(string message)
        : base(message)
    {
    }

    public JobReceiptExpiredException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public static JobReceiptExpiredException For(JobReceipt receipt)
    {
        ArgumentNullException.ThrowIfNull(receipt);
        return new($"The receipt for message '{receipt.MessageId}' has expired.") { MessageId = receipt.MessageId };
    }

    public string? MessageId { get; private init; }
}
