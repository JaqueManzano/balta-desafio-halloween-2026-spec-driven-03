using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Shortener.IntegrationTests.Infrastructure;

namespace Shortener.IntegrationTests;

public sealed class RedirectEndpointsTests
{
    [Fact]
    public async Task Public_redirect_counts_visits_and_reports_missing_or_expired_links()
    {
        var database = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.db");
        using var factory = new ApiFactory(database);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        await LinkCreationTests.AuthenticateAsync(client);
        var linkResponse = await client.PostAsJsonAsync("/api/links",
            new { targetUrl = "https://example.com/redirect", code = "public" });
        var link = await linkResponse.Content.ReadFromJsonAsync<LinkCreationTests.LinkResponse>();
        Assert.NotNull(link);

        var beforeRedirect = await client.GetFromJsonAsync<LinkCreationTests.LinkResponse>($"/api/links/{link.Code}/stats");
        Assert.NotNull(beforeRedirect);
        Assert.Equal(link.Code, beforeRedirect.Code);
        using var redirect = await client.GetAsync($"/{link.Code}");
        Assert.Equal(HttpStatusCode.Found, redirect.StatusCode);
        Assert.Equal("https://example.com/redirect", redirect.Headers.Location?.ToString());

        using var missing = await client.GetAsync("/missing");
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);

        var stats = await client.GetFromJsonAsync<LinkCreationTests.LinkResponse>($"/api/links/{link.Code}/stats");
        Assert.NotNull(stats);
        Assert.Equal(1, stats.AccessCount);
    }

    [Fact]
    public async Task Expired_link_returns_gone_without_incrementing_access_count()
    {
        var database = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.db");
        using var factory = new ApiFactory(database);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        await LinkCreationTests.AuthenticateAsync(client);
        var response = await client.PostAsJsonAsync("/api/links",
            new { targetUrl = "https://example.com/expired", code = "expired", expiresAtUtc = DateTimeOffset.UtcNow.AddMilliseconds(500) });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var link = await response.Content.ReadFromJsonAsync<LinkCreationTests.LinkResponse>();
        Assert.NotNull(link);

        await Task.Delay(750);
        using var redirect = await client.GetAsync($"/{link.Code}");
        Assert.Equal(HttpStatusCode.Gone, redirect.StatusCode);
    }
}
