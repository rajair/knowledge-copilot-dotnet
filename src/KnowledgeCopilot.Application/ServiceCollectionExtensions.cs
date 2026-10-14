using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace KnowledgeCopilot.Application;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers profile-independent core services. Use cases are added here from Step 3; adapters are
    /// registered separately by the composition root.
    /// </summary>
    public static IServiceCollection AddKnowledgeCopilotCore(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton(TimeProvider.System);

        return services;
    }
}
