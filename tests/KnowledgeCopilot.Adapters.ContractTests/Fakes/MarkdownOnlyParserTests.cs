using System.Text;
using KnowledgeCopilot.Adapters.Fakes;

namespace KnowledgeCopilot.Adapters.ContractTests.Fakes;

public sealed class MarkdownOnlyParserTests
{
    private readonly MarkdownOnlyParser _parser = new();

    [Theory]
    [InlineData("text/markdown", true)]
    [InlineData("text/markdown; charset=utf-8", true)]
    [InlineData("TEXT/X-MARKDOWN", true)]
    [InlineData("application/pdf", false)]
    public void CanParse_ContentType_MatchesMarkdownOnly(string contentType, bool expected)
    {
        Assert.Equal(expected, _parser.CanParse(contentType));
    }

    [Fact]
    public async Task Parse_NestedHeadings_TracksHeadingPath()
    {
        const string Markdown = "Intro text\n# Benefits\nOverview\n## Leave\nParental leave is 16 weeks.\n# Travel\nBook early.\n";

        var sections = await ParseAsync(Markdown);

        Assert.Equal(
            [
                (string.Empty, "Intro text"),
                ("Benefits", "Overview"),
                ("Benefits > Leave", "Parental leave is 16 weeks."),
                ("Travel", "Book early."),
            ],
            sections.Select(s => (string.Join(" > ", s.HeadingPath), s.Text)));
        Assert.All(sections, s => Assert.False(s.IsTable));
    }

    [Fact]
    public async Task Parse_HashWithoutSpace_IsBodyText()
    {
        var section = Assert.Single(await ParseAsync("#hashtag stays text"));

        Assert.Empty(section.HeadingPath);
        Assert.Equal("#hashtag stays text", section.Text);
    }

    [Fact]
    public async Task Parse_UnsupportedContentType_ThrowsNotSupportedException()
    {
        using var content = new MemoryStream(Encoding.UTF8.GetBytes("x"));

        await Assert.ThrowsAsync<NotSupportedException>(
            () => _parser.ParseAsync(content, "application/pdf", TestContext.Current.CancellationToken));
    }

    private async Task<IReadOnlyList<KnowledgeCopilot.Domain.Ports.ParsedSection>> ParseAsync(string markdown)
    {
        using var content = new MemoryStream(Encoding.UTF8.GetBytes(markdown));
        return await _parser.ParseAsync(content, "text/markdown", TestContext.Current.CancellationToken);
    }
}
