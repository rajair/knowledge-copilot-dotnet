using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace KnowledgeCopilot.Api.OpenApi;

/// <summary>
/// The generator lists the discriminator as required on the base schema only. Generators read each variant
/// on its own, so without this the TS type and Pydantic model get an optional <c>type</c>, which breaks
/// discriminated unions (D-1-04). Variants are also marked <c>type: object</c>.
/// </summary>
internal sealed class DiscriminatorRequiredTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        var schemas = document.Components?.Schemas;
        if (schemas is null)
        {
            return Task.CompletedTask;
        }

        foreach (var baseSchema in schemas.Values)
        {
            if (baseSchema.Discriminator is not { PropertyName: { } property, Mapping: { } mapping })
            {
                continue;
            }

            foreach (var reference in mapping.Values)
            {
                if (reference.Reference.Id is { } id && schemas.TryGetValue(id, out var target) && target is OpenApiSchema variant)
                {
                    variant.Type ??= JsonSchemaType.Object;
                    variant.Required ??= new SortedSet<string>(StringComparer.Ordinal);
                    variant.Required.Add(property);
                }
            }
        }

        return Task.CompletedTask;
    }
}
