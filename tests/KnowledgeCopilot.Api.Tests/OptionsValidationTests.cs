using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Options;

namespace KnowledgeCopilot.Api.Tests;

public sealed class OptionsValidationTests
{
    [Fact]
    public void Startup_ProfileMissing_ThrowsOptionsValidationException()
    {
        using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.UseEnvironment("Production"));

        var exception = Assert.Throws<OptionsValidationException>(() => factory.CreateClient());

        Assert.Contains("Profile", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("Nope")]
    [InlineData("42")]
    public void Startup_ProfileInvalid_Throws(string profile)
    {
        using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder
                .UseEnvironment("Production")
                .UseSetting("KnowledgeCopilot:Profile", profile));

        Assert.ThrowsAny<Exception>(() => factory.CreateClient());
    }
}
