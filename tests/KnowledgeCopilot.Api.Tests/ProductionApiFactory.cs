using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace KnowledgeCopilot.Api.Tests;

/// <summary>Hosts the Api in the Production environment with a valid profile supplied through configuration.</summary>
public sealed class ProductionApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Production");
        builder.UseSetting("KnowledgeCopilot:Profile", "Free");
    }
}
