using System.Text.Json;

namespace KnowledgeCopilot.Contracts.Serialization;

/// <summary>Applies the contract serializer settings to host-owned options (for example ASP.NET Core HTTP JSON).</summary>
public static class ContractsJson
{
    /// <summary>
    /// Copies the <see cref="ContractsJsonContext"/> settings onto <paramref name="options"/> and puts the
    /// source-generated resolver first, so HTTP payloads and generated schemas follow the same rules.
    /// </summary>
    public static void Apply(JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var source = ContractsJsonContext.Default.Options;
        options.PropertyNamingPolicy = source.PropertyNamingPolicy;
        options.PropertyNameCaseInsensitive = source.PropertyNameCaseInsensitive;
        options.NumberHandling = source.NumberHandling;
        options.RespectNullableAnnotations = source.RespectNullableAnnotations;
        options.RespectRequiredConstructorParameters = source.RespectRequiredConstructorParameters;
        options.AllowOutOfOrderMetadataProperties = source.AllowOutOfOrderMetadataProperties;
        options.TypeInfoResolverChain.Insert(0, ContractsJsonContext.Default);
    }
}
