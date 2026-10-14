using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;

namespace KnowledgeCopilot.Api.Tests;

/// <summary>
/// Hosts the Api the way the AppHost runs it until Step 3: Development environment, Free profile, in-memory
/// fakes (D-2-03). The container validates every registration and scope at build time.
/// </summary>
public sealed class FakesApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(Environments.Development);
        builder.UseSetting("KnowledgeCopilot:Profile", "Free");
        builder.UseSetting("KnowledgeCopilot:UseFakes", "true");
        builder.UseDefaultServiceProvider(options =>
        {
            options.ValidateOnBuild = true;
            options.ValidateScopes = true;
        });
    }
}
