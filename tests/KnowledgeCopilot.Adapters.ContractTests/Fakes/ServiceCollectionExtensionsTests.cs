using KnowledgeCopilot.Adapters.Fakes;
using KnowledgeCopilot.Domain.Ports;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace KnowledgeCopilot.Adapters.ContractTests.Fakes;

public sealed class ServiceCollectionExtensionsTests
{
    [Theory]
    [InlineData("Production")]
    [InlineData("Staging")]
    [InlineData("Test")]
    public void AddFakeAdapters_OutsideDevelopment_Throws(string environment)
    {
        var builder = CreateBuilder(environment);

        var exception = Assert.Throws<InvalidOperationException>(() => builder.AddFakeAdapters());

        Assert.Contains("Development", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("Production")]
    [InlineData("Staging")]
    public void AddDevCurrentPrincipal_OutsideDevelopment_Throws(string environment)
    {
        var builder = CreateBuilder(environment);

        Assert.Throws<InvalidOperationException>(() => builder.AddDevCurrentPrincipal());
        Assert.DoesNotContain(builder.Services, d => d.ServiceType == typeof(ICurrentPrincipal));
    }

    [Fact]
    public void AddFakeAdapters_Development_ResolvesDevPrincipalWithConfiguredGroups()
    {
        var builder = CreateBuilder(Environments.Development);
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["KnowledgeCopilot:DevPrincipal:Groups:0"] = "hr",
            ["KnowledgeCopilot:DevPrincipal:Groups:1"] = "eng",
        });
        builder.AddFakeAdapters();
        using var provider = builder.Services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        using var scope = provider.CreateScope();

        var principal = scope.ServiceProvider.GetRequiredService<ICurrentPrincipal>();

        Assert.Equal(["g:eng", "g:hr", "t:dev", "u:dev-user"], principal.ToAclPrincipals());
    }

    private static HostApplicationBuilder CreateBuilder(string environment) =>
        Host.CreateEmptyApplicationBuilder(new HostApplicationBuilderSettings { EnvironmentName = environment });
}
