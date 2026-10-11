namespace KnowledgeCopilot.Contracts.Answers;

/// <summary>Non-streaming question (evals and integrations).</summary>
public sealed record AnswerRequest
{
    /// <summary>The user's question.</summary>
    public required string Question { get; init; }

    /// <summary>Continue an existing conversation, if set.</summary>
    public Guid? ConversationId { get; init; }
}
