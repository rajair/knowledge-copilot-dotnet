namespace KnowledgeCopilot.Contracts.Widgets;

/// <summary>
/// An action the model proposes. It never runs without a server-verified approval (D-15).
/// The action type is the discriminator of <c>arguments</c>.
/// </summary>
public sealed record ActionProposalWidget : Widget
{
    /// <summary>One-sentence description shown on the approval card.</summary>
    public required string Summary { get; init; }

    /// <summary>Typed arguments; their <c>type</c> names the action.</summary>
    public required ActionArguments Arguments { get; init; }
}
