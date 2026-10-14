namespace KnowledgeCopilot.Contracts.Documents;

/// <summary>Metadata sent with a document upload, as the <c>metadata</c> part of the multipart request.</summary>
public sealed record DocumentUploadMetadata
{
    /// <summary>Display title used in citations.</summary>
    public required string Title { get; init; }

    /// <summary>Where the document came from (URL or path), if known.</summary>
    public string? SourceUri { get; init; }

    /// <summary>MIME type of the uploaded file, for example <c>text/markdown</c> or <c>application/pdf</c>.</summary>
    public required string ContentType { get; init; }

    /// <summary>Initial access control list.</summary>
    public required AclRequest Acl { get; init; }
}
