namespace KnowledgeCopilot.Contracts.Answers;

/// <summary>Cost and latency of one answer.</summary>
public sealed record UsageDto
{
    /// <summary>Prompt tokens.</summary>
    public required int InputTokens { get; init; }

    /// <summary>Completion tokens.</summary>
    public required int OutputTokens { get; init; }

    /// <summary>Estimated cost in US dollars (decimal in C#, a JSON number on the wire).</summary>
    public required decimal CostUsd { get; init; }

    /// <summary>Time to first token, in milliseconds.</summary>
    public required int TtftMs { get; init; }

    /// <summary>Retrieval time, in milliseconds.</summary>
    public required int RetrievalMs { get; init; }
}
