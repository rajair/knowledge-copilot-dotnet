using System.ComponentModel.DataAnnotations;

namespace KnowledgeCopilot.ServiceDefaults;

/// <summary>Which set of adapters the hosts compose (D-02).</summary>
public enum KnowledgeCopilotProfile
{
    Free,
    Azure,
}

/// <summary>Root options bound from the <c>KnowledgeCopilot</c> configuration section.</summary>
public sealed class KnowledgeCopilotOptions
{
    public const string SectionName = "KnowledgeCopilot";

    /// <summary>
    /// Required, with no default: it comes only from the AppHost, environment variables or a test host.
    /// Nullable so that a missing value fails validation instead of silently becoming <see cref="KnowledgeCopilotProfile.Free"/>.
    /// </summary>
    [Required]
    [EnumDataType(typeof(KnowledgeCopilotProfile))]
    public KnowledgeCopilotProfile? Profile { get; set; }

    /// <summary>
    /// Compose the in-memory fakes instead of the profile's adapters. Development only (D-2-01): the hosts
    /// refuse to start with it in any other environment.
    /// </summary>
    public bool UseFakes { get; set; }
}
