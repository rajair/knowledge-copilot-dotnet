using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace KnowledgeCopilot.Api.OpenApi;

/// <summary>
/// Puts contracts that no endpoint returns (widgets travel over SSE; upload metadata is a multipart part)
/// into <c>components.schemas</c> the same way endpoint types get there, including their nested and
/// derived types. It adds one API description per type that has no route (nothing is mapped, so nothing
/// can be called) and <see cref="RemoveCarrierPaths"/> deletes those paths from the document (D-1-04).
/// </summary>
internal sealed class AdditionalSchemas(IReadOnlyList<Type> types) : IApiDescriptionProvider
{
    private const string CarrierPrefix = "__schema-carrier/";
    private const string CarrierTag = "__schema-carrier";

    // Run after the endpoint provider (order -1100) so carriers are appended, not interleaved.
    public int Order => 0;

    public void OnProvidersExecuting(ApiDescriptionProviderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        foreach (var type in types)
        {
            var description = new ApiDescription
            {
                ActionDescriptor = new ActionDescriptor
                {
                    DisplayName = $"Schema carrier for {type.Name}",
                    EndpointMetadata = [new TagsAttribute(CarrierTag)],
                },
                HttpMethod = HttpMethods.Get,
                RelativePath = CarrierPrefix + type.Name,
            };
            description.SupportedResponseTypes.Add(new ApiResponseType
            {
                StatusCode = StatusCodes.Status200OK,
                Type = type,
                ApiResponseFormats = { new ApiResponseFormat { MediaType = "application/json" } },
            });
            context.Results.Add(description);
        }
    }

    public void OnProvidersExecuted(ApiDescriptionProviderContext context)
    {
    }

    public static Task RemoveCarrierPaths(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        foreach (var path in document.Paths.Keys.Where(path => path.StartsWith("/" + CarrierPrefix, StringComparison.Ordinal)).ToList())
        {
            document.Paths.Remove(path);
        }

        foreach (var tag in document.Tags?.Where(tag => tag.Name == CarrierTag).ToList() ?? [])
        {
            document.Tags!.Remove(tag);
        }

        return Task.CompletedTask;
    }
}
