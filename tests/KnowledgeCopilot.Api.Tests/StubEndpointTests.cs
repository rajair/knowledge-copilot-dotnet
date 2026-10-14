using System.Net;
using System.Net.Http.Json;
using System.Text;

namespace KnowledgeCopilot.Api.Tests;

public sealed class StubEndpointTests(ProductionApiFactory factory) : IClassFixture<ProductionApiFactory>
{
    [Fact]
    public async Task PostSearch_ValidRequest_Returns501ProblemDetails()
    {
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync(
            new Uri("/api/v1/search", UriKind.Relative),
            new { query = "revenue 2025" },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotImplemented, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task PostSearch_QuotedNumber_Returns400()
    {
        using var client = factory.CreateClient();
        using var body = new StringContent("""{"query":"q","topK":"5"}""", Encoding.UTF8, "application/json");

        using var response = await client.PostAsync(new Uri("/api/v1/search", UriKind.Relative), body, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
