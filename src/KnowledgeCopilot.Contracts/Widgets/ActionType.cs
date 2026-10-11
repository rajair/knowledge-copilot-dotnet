using System.Text.Json.Serialization;
using KnowledgeCopilot.Contracts.Serialization;

namespace KnowledgeCopilot.Contracts.Widgets;

/// <summary>
/// Actions the assistant can propose. Each value's camelCase name is the discriminator of one
/// <see cref="ActionArguments"/> subtype (a test enforces this).
/// </summary>
[JsonConverter(typeof(CamelCaseEnumConverter<ActionType>))]
public enum ActionType
{
    /// <summary>Create a follow-up task.</summary>
    CreateFollowUpTask,

    /// <summary>Draft (not send) an email.</summary>
    DraftEmail,
}
