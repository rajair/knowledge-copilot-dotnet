namespace KnowledgeCopilot.Api.Endpoints;

/// <summary>
/// Streaming chat route (architecture section 10). Its request and the AI SDK UI message stream are
/// defined in Step 5, so the stub only declares the SSE media type.
/// </summary>
internal static class ChatEndpoints
{
    public static RouteGroupBuilder MapChatEndpoints(this RouteGroupBuilder api)
    {
        api.MapPost("/chat", Chat)
            .WithTags("Chat")
            .WithName("Chat")
            .Produces(StatusCodes.Status200OK, contentType: "text/event-stream");
        return api;
    }

    private static ProblemHttpResult Chat() => NotImplementedYet.Problem(5);
}
