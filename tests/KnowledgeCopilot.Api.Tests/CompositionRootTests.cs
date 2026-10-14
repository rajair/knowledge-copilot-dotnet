using KnowledgeCopilot.Adapters.Fakes;
using KnowledgeCopilot.Domain.Ports;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace KnowledgeCopilot.Api.Tests;

public sealed class CompositionRootTests(FakesApiFactory factory) : IClassFixture<FakesApiFactory>
{
    public static TheoryData<Type> Ports =>
    [
        typeof(IIndexStore),
        typeof(IReranker),
        typeof(IBlobStore),
        typeof(IWorkQueue),
        typeof(IDocumentParser),
        typeof(ICurrentPrincipal),
        typeof(IChatClient),
        typeof(IEmbeddingGenerator<string, Embedding<float>>),
        typeof(TimeProvider),
    ];

    [Theory]
    [MemberData(nameof(Ports))]
    public void Resolve_DevelopmentWithFakes_ResolvesPort(Type port)
    {
        using var scope = factory.Services.CreateScope();

        Assert.NotNull(scope.ServiceProvider.GetRequiredService(port));
    }

    [Fact]
    public void Resolve_DevelopmentWithFakes_UsesDevCurrentPrincipal()
    {
        using var scope = factory.Services.CreateScope();

        var principal = scope.ServiceProvider.GetRequiredService<ICurrentPrincipal>();

        Assert.IsType<DevCurrentPrincipal>(principal);
        Assert.Equal(DevCurrentPrincipal.DevTenantId, principal.TenantId);
    }

    [Theory]
    [InlineData("Production")]
    [InlineData("Staging")]
    public void Start_UseFakesOutsideDevelopment_FailsWithClearMessage(string environment)
    {
        var exception = StartFails(environment, profile: "Free", useFakes: true);

        Assert.Contains("only allowed in the Development environment", exception.Message, StringComparison.Ordinal);
        Assert.Contains(environment, exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("Production", "Free")]
    [InlineData("Development", "Free")]
    [InlineData("Production", "Azure")]
    public void Start_RealProfileBeforeStep3_FailsWithClearMessage(string environment, string profile)
    {
        var exception = StartFails(environment, profile, useFakes: false);

        Assert.Contains($"Profile '{profile}' adapters", exception.Message, StringComparison.Ordinal);
    }

    private static InvalidOperationException StartFails(string environment, string profile, bool useFakes)
    {
        using var app = new ConfiguredApiFactory(environment, profile, useFakes);
        return Assert.Throws<InvalidOperationException>(() => app.Services);
    }

    private sealed class ConfiguredApiFactory(string environment, string profile, bool useFakes)
        : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment(environment);
            builder.UseSetting("KnowledgeCopilot:Profile", profile);
            builder.UseSetting("KnowledgeCopilot:UseFakes", useFakes ? "true" : "false");
        }
    }
}
