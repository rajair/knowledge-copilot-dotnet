using System.Net;
using System.Text.Json.Nodes;
using KnowledgeCopilot.Contracts;
using Microsoft.AspNetCore.Hosting;

namespace KnowledgeCopilot.Api.Tests;

public sealed class OpenApiDocumentTests(ProductionApiFactory factory) : IClassFixture<ProductionApiFactory>
{
    private static readonly Uri DocumentUri = new("/openapi/v1.json", UriKind.Relative);

    [Fact]
    public async Task GetDocument_ProductionEnvironment_Returns404()
    {
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(DocumentUri, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetDocument_DevelopmentEnvironment_HasContractsVersionAndNoCarrierPaths()
    {
        using var development = factory.WithWebHostBuilder(builder => builder.UseEnvironment("Development"));
        using var client = development.CreateClient();

        var document = JsonNode.Parse(await client.GetStringAsync(DocumentUri, TestContext.Current.CancellationToken))!;

        Assert.Equal(ContractsVersion.Value, document["info"]!["version"]!.GetValue<string>());
        var paths = document["paths"]!.AsObject().Select(path => path.Key).ToList();
        Assert.Contains("/api/v1/search", paths);
        Assert.DoesNotContain(paths, path => path.Contains("schema-carrier", StringComparison.Ordinal));
        Assert.NotNull(document["components"]!["schemas"]!["Widget"]);
        Assert.NotNull(document["components"]!["schemas"]!["DocumentUploadMetadata"]);
    }

    [Fact]
    public async Task GetSchemaCarrierPath_AnyEnvironment_IsNotRoutable()
    {
        using var development = factory.WithWebHostBuilder(builder => builder.UseEnvironment("Development"));
        using var client = development.CreateClient();

        using var response = await client.GetAsync(new Uri("/__schema-carrier/Widget", UriKind.Relative), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
