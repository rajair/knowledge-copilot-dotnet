using System.Text.Json.Serialization;
using KnowledgeCopilot.Contracts.Serialization;

namespace KnowledgeCopilot.Contracts.Documents;

/// <summary>Lifecycle state of a document.</summary>
[JsonConverter(typeof(CamelCaseEnumConverter<DocumentStatus>))]
public enum DocumentStatus
{
    /// <summary>Stored and queued for indexing.</summary>
    Pending,

    /// <summary>A worker is indexing it.</summary>
    Processing,

    /// <summary>Searchable.</summary>
    Indexed,

    /// <summary>Indexing failed; see <c>lastError</c>.</summary>
    Failed,

    /// <summary>Delete requested; chunks are being removed.</summary>
    Deleting,

    /// <summary>Removed from the index and storage.</summary>
    Deleted,
}
