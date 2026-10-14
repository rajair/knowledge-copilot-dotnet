using System.Reflection;

namespace KnowledgeCopilot.Api.OpenApi;

/// <summary>
/// <c>Microsoft.Extensions.ApiDescription.Server</c> writes <c>contracts/schema/openapi.json</c> on build by
/// starting this host inside <c>GetDocument.Insider</c>. The document does not depend on the profile, so
/// that run gets a fixed one instead of failing startup validation (D-1-04).
/// </summary>
internal static class BuildTimeDocument
{
    public static bool IsGenerating =>
        string.Equals(Assembly.GetEntryAssembly()?.GetName().Name, "GetDocument.Insider", StringComparison.Ordinal);

    public static void UseFixedProfile(IConfigurationBuilder configuration) =>
        configuration.AddInMemoryCollection([new("KnowledgeCopilot:Profile", "Free")]);
}
