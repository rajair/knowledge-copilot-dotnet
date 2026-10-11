using KnowledgeCopilot.Contracts.Search;

namespace KnowledgeCopilot.Api.Endpoints;

/// <summary>Debug retrieval route (architecture section 10). Stub until Step 4.</summary>
internal static class SearchEndpoints
{
    public static RouteGroupBuilder MapSearchEndpoints(this RouteGroupBuilder api)
    {
        api.MapPost("/search", Search).WithTags("Search").WithName("Search");
        return api;
    }

    private static Results<Ok<SearchResponse>, ProblemHttpResult> Search(SearchRequest request) =>
        NotImplementedYet.Problem(4);
}
