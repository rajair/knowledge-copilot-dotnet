using Microsoft.Extensions.Hosting;

namespace KnowledgeCopilot.Adapters.Azure;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the Azure-profile adapters (AI Search, Azure OpenAI, Blob, Storage Queue…). They arrive with
    /// the Azure profile; until then this fails fast so a host never starts with ports left unregistered.
    /// </summary>
    public static IHostApplicationBuilder AddAzureAdapters(this IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        throw new InvalidOperationException(
            "Profile 'Azure' adapters are not implemented yet (Steps 3-4 add them alongside Free). For local development set KnowledgeCopilot:UseFakes=true in the Development environment.");
    }
}
