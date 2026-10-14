namespace KnowledgeCopilot.Domain.Ports;

/// <summary>Turns a document into heading-aware sections (D-10).</summary>
public interface IDocumentParser
{
    bool CanParse(string contentType);

    /// <exception cref="NotSupportedException"><see cref="CanParse"/> is false for <paramref name="contentType"/>.</exception>
    Task<IReadOnlyList<ParsedSection>> ParseAsync(Stream content, string contentType, CancellationToken cancellationToken);
}

/// <summary>A run of text under one heading path.</summary>
/// <param name="HeadingPath">Headings from the top level down to this section; empty before the first heading.</param>
/// <param name="Text">Section body.</param>
/// <param name="Page">1-based page for paginated formats, otherwise null.</param>
/// <param name="IsTable">The section is a table, which the chunker keeps whole.</param>
public sealed record ParsedSection(IReadOnlyList<string> HeadingPath, string Text, int? Page, bool IsTable);
