using System.Net;
using Aspire.Hosting.Testing;

namespace KnowledgeCopilot.AppHost.Tests;

public sealed class AppHostSmokeTests
{
    private static readonly TimeSpan StartupTimeout = TimeSpan.FromMinutes(3);

    [Fact]
    public async Task Start_FreeProfile_ApiBecomesHealthy()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(StartupTimeout);

        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.KnowledgeCopilot_AppHost>(timeout.Token);
        await using var app = await appHost.BuildAsync(timeout.Token);
        await app.StartAsync(timeout.Token);

        await app.ResourceNotifications.WaitForResourceHealthyAsync("api", timeout.Token);

        using var client = app.CreateHttpClient("api");
        using var response = await client.GetAsync(new Uri("/health", UriKind.Relative), timeout.Token);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
