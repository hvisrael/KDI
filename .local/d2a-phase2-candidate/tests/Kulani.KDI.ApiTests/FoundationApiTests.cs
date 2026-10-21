using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Kulani.KDI.Contracts.Foundation.V1;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Kulani.KDI.ApiTests;

public sealed class FoundationApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> factory;
    private readonly HttpClient client;

    public FoundationApiTests(WebApplicationFactory<Program> factory)
    {
        this.factory = factory;
        client = factory.CreateClient();
    }

    [Fact]
    public async Task Liveness_is_public_and_does_not_claim_readiness()
    {
        var response = await client.GetAsync("/health/live", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<LivenessResponse>(TestContext.Current.CancellationToken);
        Assert.Equal("healthy", body!.Status);
        Assert.False(response.Headers.Contains("Server"));
    }

    [Fact]
    public async Task Metadata_has_only_the_documented_fields()
    {
        var body = await client.GetFromJsonAsync<JsonElement>("/api/v1/system/info", TestContext.Current.CancellationToken);
        Assert.Equal(new[] { "apiVersion", "product" }, body.EnumerateObject().Select(p => p.Name).Order().ToArray());
        Assert.Equal("v1", body.GetProperty("apiVersion").GetString());
    }

    [Theory]
    [InlineData("/api/v1/organizations/00000000-0000-0000-0000-000000000001/clients")]
    [InlineData("/missing")]
    public async Task Unknown_routes_are_correlated_problem_details(string path)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Add("X-Correlation-ID", "test-request-123");
        var response = await client.SendAsync(request, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        Assert.Equal("not_found", body.GetProperty("code").GetString());
        Assert.Equal("test-request-123", body.GetProperty("correlationId").GetString());
        Assert.Equal("test-request-123", response.Headers.GetValues("X-Correlation-ID").Single());
    }

    [Fact]
    public async Task OpenApi_exposes_only_the_implemented_foundation_operations()
    {
        var doc = await client.GetFromJsonAsync<JsonElement>("/openapi/v1.json", TestContext.Current.CancellationToken);
        var paths = doc.GetProperty("paths");
        Assert.Equal(new[] { "/api/v1/system/info", "/health/live", "/health/ready" }, paths.EnumerateObject().Select(p => p.Name).Order().ToArray());
        foreach (var path in paths.EnumerateObject())
        {
            var operation = path.Value.GetProperty("get");
            var schema = operation.GetProperty("responses").GetProperty("200")
                .GetProperty("content").GetProperty("application/json").GetProperty("schema");
            Assert.True(schema.TryGetProperty("$ref", out _) || schema.TryGetProperty("properties", out _));
        }
    }

    [Fact]
    public async Task Readiness_fails_closed_without_runtime_configuration()
    {
        var response = await client.GetAsync("/health/ready", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.Equal("{\"status\":\"not_ready\"}", body);
        Assert.DoesNotContain("Server", body);
    }

    [Fact]
    public async Task Exception_response_redacts_sensitive_details_even_in_development()
    {
        using var failing = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            // Keep this failure-injection host independent of Windows EventLog permissions.
            builder.ConfigureLogging(logging => logging.ClearProviders().AddConsole());
            builder.ConfigureServices(services => services.Configure<OpenApiOptions>("v1", options =>
                options.AddDocumentTransformer((_, _, _) => throw new InvalidOperationException("synthetic-sensitive-detail"))));
        });
        using var errorClient = failing.CreateClient();
        var response = await errorClient.GetAsync("/openapi/v1.json", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        var text = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.DoesNotContain("synthetic-sensitive-detail", text);
        Assert.DoesNotContain("stackTrace", text);
        var problem = JsonSerializer.Deserialize<JsonElement>(text);
        Assert.Equal(response.Headers.GetValues("X-Correlation-ID").Single(), problem.GetProperty("correlationId").GetString());
    }

}
