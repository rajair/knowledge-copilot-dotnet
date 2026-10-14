namespace KnowledgeCopilot.Api.Endpoints;

/// <summary>RFC 9457 body for endpoints whose behaviour lands in a later step.</summary>
internal static class NotImplementedYet
{
    public static ProblemHttpResult Problem(int step) =>
        TypedResults.Problem(
            statusCode: StatusCodes.Status501NotImplemented,
            title: "Not implemented",
            detail: $"This endpoint is implemented in Step {step}.");
}
