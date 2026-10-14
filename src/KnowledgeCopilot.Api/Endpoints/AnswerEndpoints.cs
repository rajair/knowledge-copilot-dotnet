using KnowledgeCopilot.Contracts.Answers;

namespace KnowledgeCopilot.Api.Endpoints;

/// <summary>Non-streaming answer route used by evals (architecture section 10). Stub until Step 5.</summary>
internal static class AnswerEndpoints
{
    public static RouteGroupBuilder MapAnswerEndpoints(this RouteGroupBuilder api)
    {
        api.MapPost("/answers", Answer).WithTags("Answers").WithName("Answer");
        return api;
    }

    private static Results<Ok<AnswerResponse>, ProblemHttpResult> Answer(AnswerRequest request) =>
        NotImplementedYet.Problem(5);
}
