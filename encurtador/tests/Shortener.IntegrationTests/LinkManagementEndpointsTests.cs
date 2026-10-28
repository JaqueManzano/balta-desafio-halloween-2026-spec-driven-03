using System.Net;
using System.Net.Http.Json;
using Shortener.IntegrationTests.Infrastructure;

namespace Shortener.IntegrationTests;

public sealed class LinkManagementEndpointsTests
{
    [Fact]
    public async Task Link_statistics_are_private_to_owner_and_expiration_is_validated()
    {
        var database = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.db");
        using var factory = new ApiFactory(database);
        using var owner = factory.CreateClient();
        await LinkCreationTests.AuthenticateAsync(owner);

        var created = await owner.PostAsJsonAsync("/api/links",
            new { targetUrl = "https://example.com/private", code = "private", expiresAtUtc = DateTime.UtcNow.AddDays(2) });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var link = await created.Content.ReadFromJsonAsync<LinkCreationTests.LinkResponse>();
        Assert.NotNull(link);

        var invalidExpiry = await owner.PostAsJsonAsync("/api/links",
            new { targetUrl = "https://example.com/invalid", expiresAtUtc = DateTime.UtcNow.AddMinutes(-1) });
        Assert.Equal(HttpStatusCode.BadRequest, invalidExpiry.StatusCode);

        using var other = factory.CreateClient();
        await LinkCreationTests.AuthenticateAsync(other);
        var forbiddenStats = await other.GetAsync($"/api/links/{link.Code}/stats");
        Assert.Equal(HttpStatusCode.NotFound, forbiddenStats.StatusCode);
        var ownLinks = await other.GetFromJsonAsync<List<LinkCreationTests.LinkResponse>>("/api/links");
        Assert.NotNull(ownLinks);
        Assert.Empty(ownLinks);
    }
}
