using KnowledgeCopilot.Adapters.Fakes;
using Microsoft.Extensions.AI;

namespace KnowledgeCopilot.Adapters.ContractTests.Fakes;

public sealed class ScriptedChatClientTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task GetResponse_ScriptedTurns_ReplaysInOrderAndRecordsMessages()
    {
        using var client = ScriptedChatClient.FromTexts("first", "second");

        var first = await client.GetResponseAsync("question one", cancellationToken: Ct);
        var second = await client.GetResponseAsync("question two", cancellationToken: Ct);

        Assert.Equal("first", first.Text);
        Assert.Equal("second", second.Text);
        Assert.Equal(["question one", "question two"], client.ReceivedMessages.Select(m => Assert.Single(m).Text));
    }

    [Fact]
    public async Task GetStreamingResponse_ScriptedUpdates_YieldsEachUpdate()
    {
        using var client = new ScriptedChatClient(
        [
            [new ChatResponseUpdate(ChatRole.Assistant, "Hel"), new ChatResponseUpdate(ChatRole.Assistant, "lo")],
        ]);

        var updates = new List<string>();
        await foreach (var update in client.GetStreamingResponseAsync("hi", cancellationToken: Ct))
        {
            updates.Add(update.Text);
        }

        Assert.Equal(["Hel", "lo"], updates);
    }

    [Fact]
    public async Task GetResponse_ScriptExhausted_ThrowsInvalidOperationException()
    {
        using var client = ScriptedChatClient.FromTexts("only");
        await client.GetResponseAsync("one", cancellationToken: Ct);

        await Assert.ThrowsAsync<InvalidOperationException>(() => client.GetResponseAsync("two", cancellationToken: Ct));
    }
}
