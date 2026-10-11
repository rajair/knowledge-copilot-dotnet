namespace KnowledgeCopilot.Contracts.Search;

/// <summary>Debug retrieval request (dev tools and evals).</summary>
/// <remarks>
/// Members with defaults are constructor parameters: source-generated deserialization resets
/// <c>init</c> property initializers to <c>default</c> when a property is absent.
/// </remarks>
/// <param name="Query">Free-text query.</param>
/// <param name="TopK">Number of hits to return.</param>
/// <param name="Mode">Retrieval path.</param>
public sealed record SearchRequest(string Query, int TopK = 8, SearchMode Mode = SearchMode.Hybrid);
