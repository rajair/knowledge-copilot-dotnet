using KnowledgeCopilot.Domain.Ports;

namespace KnowledgeCopilot.Adapters.Fakes;

/// <summary>
/// Stub <see cref="IDocumentParser"/> for Markdown: splits on ATX headings (<c>#</c> … <c>######</c>) and
/// tracks the heading path. No tables, no front matter, no setext headings; the Markdig parser arrives in
/// Step 3.
/// </summary>
public sealed class MarkdownOnlyParser : IDocumentParser
{
    private static readonly string[] SupportedContentTypes = ["text/markdown", "text/x-markdown"];

    public bool CanParse(string contentType) =>
        SupportedContentTypes.Contains(MediaType(contentType), StringComparer.OrdinalIgnoreCase);

    public async Task<IReadOnlyList<ParsedSection>> ParseAsync(Stream content, string contentType, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(content);
        if (!CanParse(contentType))
        {
            throw new NotSupportedException($"Content type '{contentType}' is not supported.");
        }

        using var reader = new StreamReader(content, leaveOpen: true);
        var text = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);

        var sections = new List<ParsedSection>();
        var headings = new List<(int Level, string Title)>();
        var body = new List<string>();

        void Flush()
        {
            var sectionText = string.Join('\n', body).Trim();
            if (sectionText.Length > 0)
            {
                sections.Add(new ParsedSection([.. headings.Select(h => h.Title)], sectionText, Page: null, IsTable: false));
            }

            body.Clear();
        }

        foreach (var line in text.ReplaceLineEndings("\n").Split('\n'))
        {
            if (TryParseHeading(line, out var level, out var title))
            {
                Flush();
                headings.RemoveAll(h => h.Level >= level);
                headings.Add((level, title));
            }
            else
            {
                body.Add(line);
            }
        }

        Flush();
        return sections;
    }

    private static string MediaType(string contentType)
    {
        ArgumentNullException.ThrowIfNull(contentType);
        var separator = contentType.IndexOf(';', StringComparison.Ordinal);
        return (separator < 0 ? contentType : contentType[..separator]).Trim();
    }

    private static bool TryParseHeading(string line, out int level, out string title)
    {
        level = line.TakeWhile(c => c == '#').Count();
        title = string.Empty;
        if (level is < 1 or > 6 || (line.Length > level && line[level] != ' '))
        {
            return false;
        }

        title = line[level..].Trim().TrimEnd('#').Trim();
        return true;
    }
}
