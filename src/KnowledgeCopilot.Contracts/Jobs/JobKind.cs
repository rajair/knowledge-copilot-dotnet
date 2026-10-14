using System.Text.Json.Serialization;
using KnowledgeCopilot.Contracts.Serialization;

namespace KnowledgeCopilot.Contracts.Jobs;

/// <summary>Work a job performs. ACL updates and deletes are separate kinds (D-14).</summary>
[JsonConverter(typeof(CamelCaseEnumConverter<JobKind>))]
public enum JobKind
{
    /// <summary>Parse, chunk, embed and upsert a document.</summary>
    Index,

    /// <summary>Rewrite the ACL of a document's chunks.</summary>
    UpdateAcl,

    /// <summary>Remove a document's chunks and blobs.</summary>
    Delete,
}
