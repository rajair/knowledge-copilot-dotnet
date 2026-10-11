using KnowledgeCopilot.Contracts;
using KnowledgeCopilot.Contracts.Documents;
using KnowledgeCopilot.Contracts.Widgets;
using Microsoft.AspNetCore.Mvc.ApiExplorer;

namespace KnowledgeCopilot.Api.OpenApi;

/// <summary>OpenAPI registration. The document is the input for TS, zod and Pydantic codegen (D-1-01, D-1-02).</summary>
internal static class OpenApiSetup
{
    /// <summary>Contracts that clients need but no endpoint returns as JSON.</summary>
    private static readonly Type[] AdditionalSchemaTypes = [typeof(DocumentUploadMetadata), typeof(Widget)];

    public static IServiceCollection AddContractsOpenApi(this IServiceCollection services)
    {
        services.AddSingleton<IApiDescriptionProvider>(new AdditionalSchemas(AdditionalSchemaTypes));

        return services.AddOpenApi(options =>
        {
            options.AddSchemaTransformer((schema, _, _) =>
            {
                // XML doc descriptions keep the build machine's line endings; the file must be byte-stable.
                schema.Description = schema.Description?.ReplaceLineEndings("\n");
                return Task.CompletedTask;
            });
            options.AddOperationTransformer<JsonFormPartTransformer>();
            options.AddDocumentTransformer((document, _, _) =>
            {
                document.Info.Title = "Knowledge Copilot API";
                document.Info.Version = ContractsVersion.Value;
                return Task.CompletedTask;
            });
            options.AddDocumentTransformer(AdditionalSchemas.RemoveCarrierPaths);
            options.AddDocumentTransformer<DiscriminatorRequiredTransformer>();
        });
    }
}
