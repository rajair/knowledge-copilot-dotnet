namespace KnowledgeCopilot.Api.OpenApi;

/// <summary>Marks a multipart form part whose value is JSON of <paramref name="Type"/>, for the OpenAPI document.</summary>
internal sealed record JsonFormPartMetadata(string Name, Type Type);
