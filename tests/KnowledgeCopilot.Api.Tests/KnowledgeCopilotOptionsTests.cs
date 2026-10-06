using KnowledgeCopilot.ServiceDefaults;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace KnowledgeCopilot.Api.Tests;

// A plain host instead of WebApplicationFactory: when startup fails, the factory's deferred host
// sometimes rethrows ObjectDisposedException instead of the startup exception.
public sealed class KnowledgeCopilotOptionsTests
{
    [Fact]
    public async Task StartAsync_ProfileMissing_ThrowsOptionsValidationException()
    {
        using var host = BuildHost(profile: null);

        var exception = await Assert.ThrowsAsync<OptionsValidationException>(
            () => host.StartAsync(TestContext.Current.CancellationToken));

        Assert.Contains("Profile", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task StartAsync_ProfileUndefinedNumber_ThrowsOptionsValidationException()
    {
        using var host = BuildHost(profile: "42");

        var exception = await Assert.ThrowsAsync<OptionsValidationException>(
            () => host.StartAsync(TestContext.Current.CancellationToken));

        Assert.Contains("Profile", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task StartAsync_ProfileUnknownName_ThrowsInvalidOperationException()
    {
        using var host = BuildHost(profile: "Nope");

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => host.StartAsync(TestContext.Current.CancellationToken));

        Assert.Contains("KnowledgeCopilot:Profile", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("Free", KnowledgeCopilotProfile.Free)]
    [InlineData("Azure", KnowledgeCopilotProfile.Azure)]
    public async Task StartAsync_ProfileValid_BindsProfile(string profile, KnowledgeCopilotProfile expected)
    {
        using var host = BuildHost(profile);

        await host.StartAsync(TestContext.Current.CancellationToken);

        Assert.Equal(expected, host.Services.GetRequiredService<IOptions<KnowledgeCopilotOptions>>().Value.Profile);
        await host.StopAsync(TestContext.Current.CancellationToken);
    }

    private static IHost BuildHost(string? profile)
    {
        var builder = Host.CreateEmptyApplicationBuilder(settings: null);
        if (profile is not null)
        {
            builder.Configuration.AddInMemoryCollection(
                new Dictionary<string, string?> { ["KnowledgeCopilot:Profile"] = profile });
        }

        return builder.AddKnowledgeCopilotOptions().Build();
    }
}
