namespace KnowledgeCopilot.Domain.Ports;

/// <summary>Thrown by <see cref="IBlobStore.OpenReadAsync"/> when the key does not exist.</summary>
public sealed class BlobNotFoundException : Exception
{
    public BlobNotFoundException()
    {
    }

    public BlobNotFoundException(string message)
        : base(message)
    {
    }

    public BlobNotFoundException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public static BlobNotFoundException ForKey(string key) => new($"Blob '{key}' was not found.") { Key = key };

    public string? Key { get; private init; }
}
