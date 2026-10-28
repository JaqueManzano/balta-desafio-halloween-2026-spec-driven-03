using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Shortener.IntegrationTests.Infrastructure;

namespace Shortener.IntegrationTests;

public sealed class PersistenceTests
{
    [Fact]
    public async Task Account_link_expiration_and_access_count_survive_host_restart()
    {
        var database = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.db");
        DateTime expiresAtUtc;
        string code;
        string email;
        const string password = "StrongPassword123!";

        using (var firstFactory = new ApiFactory(database))
        using (var firstClient = firstFactory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false }))
        {
            email = $"persistent-{Guid.NewGuid():N}@example.com";
            var credentials = new { email, password };
            Assert.Equal(HttpStatusCode.Created, (await firstClient.PostAsJsonAsync("/api/accounts", credentials)).StatusCode);
            var login = await firstClient.PostAsJsonAsync("/api/accounts/login", credentials);
            var tokens = await login.Content.ReadFromJsonAsync<TokenResponse>();
            Assert.NotNull(tokens);
            firstClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokens.AccessToken);

            expiresAtUtc = DateTime.UtcNow.AddDays(4);
            var create = await firstClient.PostAsJsonAsync("/api/links",
                new { targetUrl = "https://example.com/persistent", code = "persist1", expiresAtUtc });
            Assert.Equal(HttpStatusCode.Created, create.StatusCode);
            var link = await create.Content.ReadFromJsonAsync<LinkResponse>();
            Assert.NotNull(link);
            code = link.Code;

            using var redirect = await firstClient.GetAsync($"/{code}");
            Assert.Equal(HttpStatusCode.Found, redirect.StatusCode);
        }

        using (var restartedFactory = new ApiFactory(database))
        using (var restartedClient = restartedFactory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false }))
        {
            var credentials = new { email, password };
            var login = await restartedClient.PostAsJsonAsync("/api/accounts/login", credentials);
            Assert.Equal(HttpStatusCode.OK, login.StatusCode);
            var tokens = await login.Content.ReadFromJsonAsync<TokenResponse>();
            Assert.NotNull(tokens);
            restartedClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokens.AccessToken);
            var stats = await restartedClient.GetFromJsonAsync<LinkResponse>($"/api/links/{code}/stats");
            Assert.NotNull(stats);
            Assert.Equal(expiresAtUtc, stats.ExpiresAtUtc);
            Assert.Equal(1, stats.AccessCount);
        }
    }

    private sealed record TokenResponse(string AccessToken, string RefreshToken);
    private sealed record LinkResponse(string Code, string ShortUrl, string TargetUrl, DateTime CreatedAtUtc,
        DateTime ExpiresAtUtc, long AccessCount);
}
