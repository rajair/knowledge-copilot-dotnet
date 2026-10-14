using System.Text.Json;
using System.Text.Json.Serialization;
using KnowledgeCopilot.Contracts.Answers;
using KnowledgeCopilot.Contracts.Documents;
using KnowledgeCopilot.Contracts.Jobs;
using KnowledgeCopilot.Contracts.Search;
using KnowledgeCopilot.Contracts.Widgets;

namespace KnowledgeCopilot.Contracts.Serialization;

/// <summary>
/// Source-generated serializer for every contract. Web defaults (camelCase), but numbers are strict
/// (no quoted numbers) and nullable annotations are enforced, so the exported schemas match the C# types.
/// </summary>
[JsonSourceGenerationOptions(
    JsonSerializerDefaults.Web,
    NumberHandling = JsonNumberHandling.Strict,
    RespectNullableAnnotations = true,
    RespectRequiredConstructorParameters = true,
    AllowOutOfOrderMetadataProperties = true)]
[JsonSerializable(typeof(AclRequest))]
[JsonSerializable(typeof(DocumentUploadMetadata))]
[JsonSerializable(typeof(DocumentDto))]
[JsonSerializable(typeof(IReadOnlyList<DocumentDto>))]
[JsonSerializable(typeof(SearchRequest))]
[JsonSerializable(typeof(SearchResponse))]
[JsonSerializable(typeof(AnswerRequest))]
[JsonSerializable(typeof(AnswerResponse))]
[JsonSerializable(typeof(Widget))]
[JsonSerializable(typeof(JobMessage))]
public sealed partial class ContractsJsonContext : JsonSerializerContext
{
    /// <summary>
    /// Top-level payloads: each gets a JSON Schema file and fixtures. Nested types are reached through these.
    /// </summary>
    public static IReadOnlyList<Type> RootTypes { get; } =
    [
        typeof(AclRequest),
        typeof(DocumentUploadMetadata),
        typeof(DocumentDto),
        typeof(SearchRequest),
        typeof(SearchResponse),
        typeof(AnswerRequest),
        typeof(AnswerResponse),
        typeof(Widget),
        typeof(JobMessage),
    ];
}
