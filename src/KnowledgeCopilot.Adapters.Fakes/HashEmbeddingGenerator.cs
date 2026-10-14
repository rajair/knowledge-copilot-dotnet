using System.Text;
using Microsoft.Extensions.AI;

namespace KnowledgeCopilot.Adapters.Fakes;

/// <summary>
/// Deterministic <see cref="IEmbeddingGenerator{TInput, TEmbedding}"/>: hashes lower-cased character
/// trigrams into buckets and L2-normalises the result. Texts that share words land close together, which is
/// enough for tests and the fakes profile; it carries no real semantics.
/// </summary>
public sealed class HashEmbeddingGenerator(int dimensions = HashEmbeddingGenerator.DefaultDimensions)
    : IEmbeddingGenerator<string, Embedding<float>>
{
    public const int DefaultDimensions = 256;

    private const uint FnvOffset = 2166136261;
    private const uint FnvPrime = 16777619;

    public int Dimensions { get; } = dimensions > 0
        ? dimensions
        : throw new ArgumentOutOfRangeException(nameof(dimensions), dimensions, "Dimensions must be positive.");

    public Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
        IEnumerable<string> values,
        EmbeddingGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(values);
        cancellationToken.ThrowIfCancellationRequested();

        var embeddings = new GeneratedEmbeddings<Embedding<float>>(values.Select(Embed));
        return Task.FromResult(embeddings);
    }

    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        ArgumentNullException.ThrowIfNull(serviceType);
        return serviceKey is null && serviceType.IsInstanceOfType(this) ? this : null;
    }

    public void Dispose()
    {
    }

    private Embedding<float> Embed(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var vector = new float[Dimensions];
        var normalized = $"  {text.ToUpperInvariant()}  ";
        for (var i = 0; i + 3 <= normalized.Length; i++)
        {
            vector[Hash(normalized.AsSpan(i, 3)) % (uint)Dimensions] += 1f;
        }

        var norm = MathF.Sqrt(vector.Sum(v => v * v));
        if (norm > 0)
        {
            for (var i = 0; i < vector.Length; i++)
            {
                vector[i] /= norm;
            }
        }

        return new Embedding<float>(vector);
    }

    private static uint Hash(ReadOnlySpan<char> trigram)
    {
        Span<byte> bytes = stackalloc byte[Encoding.UTF8.GetMaxByteCount(trigram.Length)];
        var count = Encoding.UTF8.GetBytes(trigram, bytes);
        var hash = FnvOffset;
        foreach (var b in bytes[..count])
        {
            hash = (hash ^ b) * FnvPrime;
        }

        return hash;
    }
}
