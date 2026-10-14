using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;

namespace KnowledgeCopilot.Adapters.Fakes;

/// <summary>
/// Deterministic <see cref="IChatClient"/> that replays scripted turns in order. Each call consumes one turn;
/// streaming yields the turn's updates one by one. Throws once the script is exhausted so a test cannot
/// silently make an unexpected extra call.
/// </summary>
public sealed class ScriptedChatClient : IChatClient
{
    private readonly Lock _gate = new();
    private readonly Queue<IReadOnlyList<ChatResponseUpdate>> _turns;
    private readonly List<IReadOnlyList<ChatMessage>> _received = [];

    public ScriptedChatClient(IEnumerable<IReadOnlyList<ChatResponseUpdate>> turns)
    {
        ArgumentNullException.ThrowIfNull(turns);
        _turns = new Queue<IReadOnlyList<ChatResponseUpdate>>(turns);
    }

    /// <summary>A client whose every turn is a single text answer.</summary>
    public static ScriptedChatClient FromTexts(params string[] answers)
    {
        ArgumentNullException.ThrowIfNull(answers);
        return new ScriptedChatClient(answers.Select(answer =>
            (IReadOnlyList<ChatResponseUpdate>)[new ChatResponseUpdate(ChatRole.Assistant, answer)]));
    }

    /// <summary>The messages passed to each call, in call order.</summary>
    public IReadOnlyList<IReadOnlyList<ChatMessage>> ReceivedMessages
    {
        get
        {
            lock (_gate)
            {
                return [.. _received];
            }
        }
    }

    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(NextTurn(messages).ToChatResponse());
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var update in NextTurn(messages))
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Yield();
            yield return update;
        }
    }

    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        ArgumentNullException.ThrowIfNull(serviceType);
        return serviceKey is null && serviceType.IsInstanceOfType(this) ? this : null;
    }

    public void Dispose()
    {
    }

    private IReadOnlyList<ChatResponseUpdate> NextTurn(IEnumerable<ChatMessage> messages)
    {
        ArgumentNullException.ThrowIfNull(messages);
        lock (_gate)
        {
            _received.Add([.. messages]);
            return _turns.TryDequeue(out var turn)
                ? turn
                : throw new InvalidOperationException("ScriptedChatClient has no scripted turns left.");
        }
    }
}
