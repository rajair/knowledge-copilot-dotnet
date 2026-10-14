using System.Text.Json.Serialization;
using KnowledgeCopilot.Contracts.Serialization;

namespace KnowledgeCopilot.Contracts.Search;

/// <summary>Which retrieval path to use.</summary>
[JsonConverter(typeof(CamelCaseEnumConverter<SearchMode>))]
public enum SearchMode
{
    /// <summary>Dense and sparse, fused with reciprocal rank fusion.</summary>
    Hybrid,

    /// <summary>Vector similarity only.</summary>
    Dense,

    /// <summary>Keyword (BM25-style) only.</summary>
    Sparse,
}
