using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Shortener.Api.Application;
using Shortener.Api.Data;
using Shortener.Api.Data.Entities;
using Shortener.Api.Data.Repositories;

namespace Shortener.UnitTests.Application;

public sealed class LinkRedirectTests
{
    [Fact]
    public async Task Redirect_increments_only_active_link_and_returns_missing_or_expired_states()
    {
        await using var fixture = await LinkFixture.CreateAsync();
        var now = DateTime.UtcNow;
        fixture.Db.Users.Add(new AppUser { Id = "owner", UserName = "owner@example.com", Email = "owner@example.com" });
        await fixture.Db.SaveChangesAsync();
        fixture.Db.Links.Add(new Link
        {
            Code = "ACTIVE",
            TargetUrl = "https://example.com/",
            CreatedAtUtc = now.AddMinutes(-1),
            ExpiresAtUtc = now.AddMinutes(1),
            OwnerId = "owner"
        });
        fixture.Db.Links.Add(new Link
        {
            Code = "EXPIRED",
            TargetUrl = "https://example.com/old",
            CreatedAtUtc = now.AddDays(-1),
            ExpiresAtUtc = now.AddMinutes(-1),
            OwnerId = "owner"
        });
        await fixture.Db.SaveChangesAsync();

        var active = await fixture.Repository.RedirectAndCountAsync("ACTIVE", now, CancellationToken.None);
        Assert.Equal(RedirectStatus.Redirected, active.Status);
        Assert.Equal("https://example.com/", active.TargetUrl);
        Assert.Equal(RedirectStatus.Expired,
            (await fixture.Repository.RedirectAndCountAsync("EXPIRED", now, CancellationToken.None)).Status);
        Assert.Equal(RedirectStatus.NotFound,
            (await fixture.Repository.RedirectAndCountAsync("UNKNOWN", now, CancellationToken.None)).Status);
        Assert.Equal(1, await fixture.Db.Links.Where(link => link.Code == "ACTIVE").Select(link => link.AccessCount).SingleAsync());
        Assert.Equal(0, await fixture.Db.Links.Where(link => link.Code == "EXPIRED").Select(link => link.AccessCount).SingleAsync());
    }

    private sealed class LinkFixture : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;
        public AppDbContext Db { get; }
        public LinkRepository Repository { get; }

        private LinkFixture(SqliteConnection connection, AppDbContext db)
        {
            _connection = connection;
            Db = db;
            Repository = new LinkRepository(db);
        }

        public static async Task<LinkFixture> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options);
            await db.Database.EnsureCreatedAsync();
            return new LinkFixture(connection, db);
        }

        public async ValueTask DisposeAsync()
        {
            await Db.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }
}
