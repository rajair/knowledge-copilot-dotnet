using System.Net;

namespace KnowledgeCopilot.Api.Tests;

public sealed class HealthEndpointTests(ProductionApiFactory factory) : IClassFixture<ProductionApiFactory>
{
    [Theory]
    [InlineData("/health")]
    [InlineData("/alive")]
    public async Task Get_ProductionEnvironment_Returns200(string path)
    {
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(new Uri(path, UriKind.Relative), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }
}
