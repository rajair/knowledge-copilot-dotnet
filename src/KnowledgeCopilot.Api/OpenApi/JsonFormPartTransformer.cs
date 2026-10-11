using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace KnowledgeCopilot.Api.OpenApi;

/// <summary>
/// Describes a multipart part that carries JSON (see <see cref="JsonFormPartMetadata"/>): the part's schema
/// becomes a reference to the contract (registered by <see cref="AdditionalSchemas"/>) and its encoding <c>application/json</c>. Minimal APIs bind it as a string.
/// </summary>
internal sealed class JsonFormPartTransformer : IOpenApiOperationTransformer
{
    private const string MultipartFormData = "multipart/form-data";

    public Task TransformAsync(
        OpenApiOperation operation,
        OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        var parts = context.Description.ActionDescriptor.EndpointMetadata.OfType<JsonFormPartMetadata>().ToList();
        if (parts.Count == 0
            || operation.RequestBody?.Content is not { } content
            || !content.TryGetValue(MultipartFormData, out var mediaType)
            || mediaType.Schema is null)
        {
            return Task.CompletedTask;
        }

        foreach (var part in parts)
        {
            var owner = FindPropertyOwner(mediaType.Schema, part.Name)
                ?? throw new InvalidOperationException($"Form part '{part.Name}' is not in the multipart schema.");
            owner.Properties![part.Name] = new OpenApiSchemaReference(part.Type.Name, context.Document);

            mediaType.Encoding ??= new Dictionary<string, OpenApiEncoding>(StringComparer.Ordinal);
            mediaType.Encoding[part.Name] = new OpenApiEncoding { ContentType = "application/json" };
        }

        return Task.CompletedTask;
    }

    private static OpenApiSchema? FindPropertyOwner(IOpenApiSchema schema, string name)
    {
        if (schema is OpenApiSchema concrete && concrete.Properties?.ContainsKey(name) == true)
        {
            return concrete;
        }

        return schema.AllOf?.Select(child => FindPropertyOwner(child, name)).FirstOrDefault(owner => owner is not null);
    }
}
