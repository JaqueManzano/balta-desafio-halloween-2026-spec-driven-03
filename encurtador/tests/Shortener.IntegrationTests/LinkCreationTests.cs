using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Shortener.IntegrationTests.Infrastructure;

namespace Shortener.IntegrationTests;

public sealed class LinkCreationTests
{
    [Fact]
    public async Task Authenticated_user_can_create_generated_and_custom_links()
    {
        var database = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.db");
        using var factory = new ApiFactory(database);
        using var client = factory.CreateClient();
        await AuthenticateAsync(client);

        var generatedResponse = await client.PostAsJsonAsync("/api/links", new { targetUrl = "https://example.com/long/path" });
        Assert.Equal(HttpStatusCode.Created, generatedResponse.StatusCode);
        var generated = await generatedResponse.Content.ReadFromJsonAsync<LinkResponse>();
        Assert.NotNull(generated);
        Assert.Matches("^[A-Za-z0-9]{7}$", generated.Code);

        var customResponse = await client.PostAsJsonAsync("/api/links",
            new { targetUrl = "https://example.com/custom", code = "MyCode9" });
        Assert.Equal(HttpStatusCode.Created, customResponse.StatusCode);
        var custom = await customResponse.Content.ReadFromJsonAsync<LinkResponse>();
        Assert.NotNull(custom);
        Assert.Equal("MYCODE9", custom.Code);
    }

    [Fact]
    public async Task Invalid_target_and_unavailable_custom_code_are_rejected()
    {
        var database = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.db");
        using var factory = new ApiFactory(database);
        using var client = factory.CreateClient();
        await AuthenticateAsync(client);

        var invalid = await client.PostAsJsonAsync("/api/links", new { targetUrl = "javascript:alert(1)" });
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);

        var request = new { targetUrl = "https://example.com/", code = "taken" };
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/api/links", request)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/links", request)).StatusCode);
    }

    internal static async Task AuthenticateAsync(HttpClient client)
    {
        var credentials = new { email = $"owner-{Guid.NewGuid():N}@example.com", password = "StrongPassword123!" };
        Assert.Equal(HttpStatusCode.Created,
            (await client.PostAsJsonAsync("/api/accounts", credentials)).StatusCode);
        var response = await client.PostAsJsonAsync("/api/accounts/login", credentials);
        response.EnsureSuccessStatusCode();
        var tokens = await response.Content.ReadFromJsonAsync<TokenResponse>();
        Assert.NotNull(tokens);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
    }

    internal sealed record LinkResponse(string Code, string ShortUrl, string TargetUrl, DateTime CreatedAtUtc,
        DateTime ExpiresAtUtc, long AccessCount);
    private sealed record TokenResponse(string AccessToken, string RefreshToken);
}
