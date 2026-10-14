namespace KnowledgeCopilot.Domain.Ports;

/// <summary>Stores original document bytes (D-05).</summary>
public interface IBlobStore
{
    /// <summary>Writes or overwrites <paramref name="key"/>.</summary>
    Task PutAsync(
        string key,
        Stream content,
        IReadOnlyDictionary<string, string> metadata,
        CancellationToken cancellationToken);

    /// <summary>Opens the stored bytes. The caller disposes the stream.</summary>
    /// <exception cref="BlobNotFoundException">No blob has that key.</exception>
    Task<Stream> OpenReadAsync(string key, CancellationToken cancellationToken);

    /// <summary>Removes the blob. Idempotent: a missing key is not an error.</summary>
    Task DeleteAsync(string key, CancellationToken cancellationToken);
}
