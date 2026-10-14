using System.Collections.Concurrent;
using KnowledgeCopilot.Domain.Ports;

namespace KnowledgeCopilot.Adapters.Fakes;

/// <summary>In-process <see cref="IBlobStore"/>. Stores a copy of the bytes, so callers cannot mutate them.</summary>
public sealed class InMemoryBlobStore : IBlobStore
{
    private readonly ConcurrentDictionary<string, StoredBlob> _blobs = new(StringComparer.Ordinal);

    public async Task PutAsync(
        string key,
        Stream content,
        IReadOnlyDictionary<string, string> metadata,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(metadata);

        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);
        _blobs[key] = new StoredBlob(buffer.ToArray(), new Dictionary<string, string>(metadata, StringComparer.Ordinal));
    }

    public Task<Stream> OpenReadAsync(string key, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!_blobs.TryGetValue(key, out var blob))
        {
            throw BlobNotFoundException.ForKey(key);
        }

        return Task.FromResult<Stream>(new MemoryStream(blob.Content, writable: false));
    }

    public Task DeleteAsync(string key, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _blobs.TryRemove(key, out _);
        return Task.CompletedTask;
    }

    private sealed record StoredBlob(byte[] Content, IReadOnlyDictionary<string, string> Metadata);
}
