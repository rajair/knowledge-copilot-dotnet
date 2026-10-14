namespace KnowledgeCopilot.Contracts.Documents;

/// <summary>A document as returned by the API.</summary>
public sealed record DocumentDto
{
    /// <summary>Document id.</summary>
    public required Guid Id { get; init; }

    /// <summary>Display title.</summary>
    public required string Title { get; init; }

    /// <summary>Where the document came from, if known.</summary>
    public string? SourceUri { get; init; }

    /// <summary>Lifecycle state.</summary>
    public required DocumentStatus Status { get; init; }

    /// <summary>Content version; increases when the content changes.</summary>
    public required int Version { get; init; }

    /// <summary>ACL version; increases when the ACL changes (D-14).</summary>
    public required int AclVersion { get; init; }

    /// <summary>Last change, with offset.</summary>
    public required DateTimeOffset UpdatedAt { get; init; }

    /// <summary>Error from the last failed job, if any.</summary>
    public string? LastError { get; init; }
}
