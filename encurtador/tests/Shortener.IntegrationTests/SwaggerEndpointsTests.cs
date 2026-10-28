using System.Text.Json;
using Shortener.IntegrationTests.Infrastructure;

namespace Shortener.IntegrationTests;

public sealed class SwaggerEndpointsTests
{
    [Fact]
    public async Task Development_swagger_ui_loads_a_supported_swagger_document()
    {
        var database = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.db");
        using var factory = new ApiFactory(database);
        using var client = factory.CreateClient();

        var documentResponse = await client.GetAsync("/swagger/v1/swagger.json");
        documentResponse.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await documentResponse.Content.ReadAsStringAsync());
        Assert.Equal("2.0", document.RootElement.GetProperty("swagger").GetString());
        var root = document.RootElement;
        Assert.Equal("apiKey", root.GetProperty("securityDefinitions").GetProperty("Bearer").GetProperty("type").GetString());
        Assert.Equal("Authorization", root.GetProperty("securityDefinitions").GetProperty("Bearer").GetProperty("name").GetString());

        var paths = root.GetProperty("paths");
        Assert.True(paths.GetProperty("/api/links").GetProperty("post").TryGetProperty("security", out _));
        Assert.False(paths.GetProperty("/{code}").GetProperty("get").TryGetProperty("security", out _));
        Assert.False(paths.GetProperty("/api/accounts/login").GetProperty("post").TryGetProperty("security", out _));

        var uiResponse = await client.GetAsync("/swagger/index.html");
        uiResponse.EnsureSuccessStatusCode();
        Assert.Contains("swagger-ui", await uiResponse.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
    }
}
