namespace KnowledgeCopilot.Contracts.Answers;

/// <summary>A complete answer with the evidence used to produce it.</summary>
public sealed record AnswerResponse
{
    /// <summary>Answer text (Markdown).</summary>
    public required string Answer { get; init; }

    /// <summary>Chunks given to the model, in prompt order.</summary>
    public required IReadOnlyList<RetrievedContext> Contexts { get; init; }

    /// <summary>Validated citations; each refers to one of <c>contexts</c>.</summary>
    public required IReadOnlyList<Citation> Citations { get; init; }

    /// <summary>Tokens, cost and latency.</summary>
    public required UsageDto Usage { get; init; }
}
