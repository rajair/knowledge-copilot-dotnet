using Microsoft.Extensions.Hosting;

namespace KnowledgeCopilot.Adapters.Free;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the Free-profile adapters (Qdrant, Ollama, MinIO, Postgres queue…). They arrive in Step 3;
    /// until then this fails fast so a host never starts with ports left unregistered.
    /// </summary>
    public static IHostApplicationBuilder AddFreeAdapters(this IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        throw new InvalidOperationException(
            "Profile 'Free' adapters arrive in Step 3. For local development set KnowledgeCopilot:UseFakes=true in the Development environment.");
    }
}
