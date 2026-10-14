using KnowledgeCopilot.Adapters.Fakes;
using Microsoft.Extensions.AI;

namespace KnowledgeCopilot.Adapters.ContractTests.Fakes;

public sealed class HashEmbeddingGeneratorTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Generate_SameText_ReturnsSameVector()
    {
        using var generator = new HashEmbeddingGenerator();

        var first = await generator.GenerateVectorAsync("Parental leave policy", cancellationToken: Ct);
        var second = await generator.GenerateVectorAsync("Parental leave policy", cancellationToken: Ct);

        Assert.Equal(first.ToArray(), second.ToArray());
    }

    [Fact]
    public async Task Generate_AnyText_ReturnsUnitVectorOfConfiguredDimensions()
    {
        using var generator = new HashEmbeddingGenerator(dimensions: 64);

        var vector = await generator.GenerateVectorAsync("Travel expenses", cancellationToken: Ct);

        Assert.Equal(64, vector.Length);
        Assert.Equal(1.0, Math.Sqrt(vector.ToArray().Sum(v => (double)v * v)), precision: 5);
    }

    [Fact]
    public async Task Generate_SimilarTexts_AreCloserThanUnrelatedText()
    {
        using var generator = new HashEmbeddingGenerator();

        var embeddings = await generator.GenerateAsync(
            ["parental leave policy", "policy on parental leave", "quarterly revenue forecast"],
            cancellationToken: Ct);

        var similar = Dot(embeddings[0].Vector, embeddings[1].Vector);
        var unrelated = Dot(embeddings[0].Vector, embeddings[2].Vector);
        Assert.True(similar > unrelated, $"similar={similar}, unrelated={unrelated}");
    }

    [Fact]
    public void Constructor_NonPositiveDimensions_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new HashEmbeddingGenerator(dimensions: 0));
    }

    private static double Dot(ReadOnlyMemory<float> a, ReadOnlyMemory<float> b)
    {
        double sum = 0;
        for (var i = 0; i < a.Length; i++)
        {
            sum += a.Span[i] * b.Span[i];
        }

        return sum;
    }
}
