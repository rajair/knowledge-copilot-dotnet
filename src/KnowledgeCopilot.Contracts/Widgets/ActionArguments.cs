using System.Text.Json.Serialization;

namespace KnowledgeCopilot.Contracts.Widgets;

/// <summary>Arguments of a proposed action. The <c>type</c> discriminator is the <see cref="ActionType"/> name.</summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type", UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FailSerialization)]
[JsonDerivedType(typeof(CreateFollowUpTaskArguments), "createFollowUpTask")]
[JsonDerivedType(typeof(DraftEmailArguments), "draftEmail")]
public abstract record ActionArguments;

/// <summary>Arguments for <see cref="ActionType.CreateFollowUpTask"/>.</summary>
public sealed record CreateFollowUpTaskArguments : ActionArguments
{
    /// <summary>Task title.</summary>
    public required string Title { get; init; }

    /// <summary>Due date, if any.</summary>
    public DateOnly? DueDate { get; init; }

    /// <summary>User id of the assignee; the caller when omitted.</summary>
    public string? Assignee { get; init; }
}

/// <summary>Arguments for <see cref="ActionType.DraftEmail"/>.</summary>
public sealed record DraftEmailArguments : ActionArguments
{
    /// <summary>Recipient addresses.</summary>
    public required IReadOnlyList<string> To { get; init; }

    /// <summary>Subject line.</summary>
    public required string Subject { get; init; }

    /// <summary>Plain-text body.</summary>
    public required string Body { get; init; }
}
