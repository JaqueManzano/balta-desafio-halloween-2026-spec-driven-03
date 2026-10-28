using System.Net;
using System.Net.Http.Json;
using Shortener.IntegrationTests.Infrastructure;

namespace Shortener.IntegrationTests;

public sealed class AccountEndpointsTests
{
    [Fact]
    public async Task Account_lifecycle_registers_logs_in_refreshes_and_logs_out()
    {
        var database = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.db");
        using var factory = new ApiFactory(database);
        using var client = factory.CreateClient();

        var credentials = new { email = "owner@example.com", password = "StrongPassword123!" };
        var registration = await client.PostAsJsonAsync("/api/accounts", credentials);
        Assert.Equal(HttpStatusCode.Created, registration.StatusCode);

        var login = await client.PostAsJsonAsync("/api/accounts/login", credentials);
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var tokens = await login.Content.ReadFromJsonAsync<TokenResponse>();
        Assert.NotNull(tokens);
        Assert.False(string.IsNullOrWhiteSpace(tokens.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(tokens.RefreshToken));

        var refresh = await client.PostAsJsonAsync("/api/accounts/refresh", new { refreshToken = tokens.RefreshToken });
        Assert.Equal(HttpStatusCode.OK, refresh.StatusCode);
        var refreshed = await refresh.Content.ReadFromJsonAsync<TokenResponse>();
        Assert.NotNull(refreshed);
        Assert.NotEqual(tokens.RefreshToken, refreshed.RefreshToken);

        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", refreshed.AccessToken);
        var logout = await client.PostAsync("/api/accounts/logout", null);
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
    }

    [Fact]
    public async Task Invalid_credentials_are_rejected_without_disclosing_which_credential_failed()
    {
        var database = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.db");
        using var factory = new ApiFactory(database);
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/accounts/login",
            new { email = "missing@example.com", password = "WrongPassword123!" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("email", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("password", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Password_shorter_than_eight_characters_is_rejected()
    {
        var database = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.db");
        using var factory = new ApiFactory(database);
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/accounts",
            new { email = "owner@example.com", password = "short" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    private sealed record TokenResponse(string AccessToken, string RefreshToken);
}
