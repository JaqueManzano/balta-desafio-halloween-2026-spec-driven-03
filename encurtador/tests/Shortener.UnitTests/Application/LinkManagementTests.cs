using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Shortener.Api.Data;
using Shortener.Api.Data.Entities;
using Shortener.Api.Data.Repositories;

namespace Shortener.UnitTests.Application;

public sealed class LinkManagementTests
{
    [Fact]
    public async Task Link_queries_are_limited_to_the_owner()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options);
        await db.Database.EnsureCreatedAsync();
        db.Users.AddRange(
            new AppUser { Id = "owner-a", UserName = "a@example.com", Email = "a@example.com" },
            new AppUser { Id = "owner-b", UserName = "b@example.com", Email = "b@example.com" });
        await db.SaveChangesAsync();
        db.Links.AddRange(
            NewLink("OWNED", "owner-a"),
            NewLink("OTHER", "owner-b"));
        await db.SaveChangesAsync();
        var repository = new LinkRepository(db);

        Assert.Single(await repository.ListByOwnerAsync("owner-a", CancellationToken.None));
        Assert.Null(await repository.FindByOwnerAsync("OTHER", "owner-a", CancellationToken.None));
        Assert.NotNull(await repository.FindByOwnerAsync("OWNED", "owner-a", CancellationToken.None));
    }

    private static Link NewLink(string code, string owner) => new()
    {
        Code = code,
        TargetUrl = "https://example.com/",
        CreatedAtUtc = DateTime.UtcNow,
        ExpiresAtUtc = DateTime.UtcNow.AddDays(1),
        OwnerId = owner
    };
}
