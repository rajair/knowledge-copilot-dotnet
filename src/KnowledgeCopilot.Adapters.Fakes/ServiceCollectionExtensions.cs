using KnowledgeCopilot.Domain.Ports;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace KnowledgeCopilot.Adapters.Fakes;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers every port with its in-memory fake and the development principal. Development only
    /// (D-2-01); throws in any other environment so fakes can never reach a deployed host.
    /// </summary>
    public static IHostApplicationBuilder AddFakeAdapters(this IHostApplicationBuilder builder)
    {
        EnsureDevelopment(builder, "UseFakes");

        var services = builder.Services;
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<IIndexStore, InMemoryIndexStore>();
        services.AddSingleton<IReranker, PassThroughReranker>();
        services.AddSingleton<IBlobStore, InMemoryBlobStore>();
        services.AddSingleton<IWorkQueue, InMemoryWorkQueue>();
        services.AddSingleton<IDocumentParser, MarkdownOnlyParser>();
        services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(_ => new HashEmbeddingGenerator());
        services.AddSingleton<IChatClient>(_ => ScriptedChatClient.FromTexts());

        return builder.AddDevCurrentPrincipal();
    }

    /// <summary>
    /// Registers <see cref="DevCurrentPrincipal"/> as the <see cref="ICurrentPrincipal"/>. Development only
    /// (D-2-01); JWT authentication replaces it in Step 8.
    /// </summary>
    public static IHostApplicationBuilder AddDevCurrentPrincipal(this IHostApplicationBuilder builder)
    {
        EnsureDevelopment(builder, nameof(DevCurrentPrincipal));

        builder.Services.AddOptions<DevPrincipalOptions>().BindConfiguration(DevPrincipalOptions.SectionName);
        builder.Services.TryAddScoped<ICurrentPrincipal, DevCurrentPrincipal>();
        return builder;
    }

    private static void EnsureDevelopment(IHostApplicationBuilder builder, string feature)
    {
        ArgumentNullException.ThrowIfNull(builder);
        if (!builder.Environment.IsDevelopment())
        {
            throw new InvalidOperationException(
                $"{feature} is only allowed in the Development environment; this host runs as '{builder.Environment.EnvironmentName}'.");
        }
    }
}
