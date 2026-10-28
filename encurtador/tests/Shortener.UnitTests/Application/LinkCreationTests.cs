using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Shortener.Api.Application;
using Shortener.Api.Data;
using Shortener.Api.Data.Entities;
using Shortener.Api.Data.Repositories;

namespace Shortener.UnitTests.Application;

public sealed class LinkCreationTests
{
    [Fact]
    public async Task Generated_and_custom_codes_are_normalized_and_expiration_defaults_to_thirty_days()
    {
        await using var fixture = await LinkFixture.CreateAsync();
        var now = DateTimeOffset.UtcNow;
        var generated = await fixture.Service.CreateAsync("https://example.com/a", null, null, "owner", CancellationToken.None);
        Assert.Equal(LinkResultStatus.Success, generated.Status);
        Assert.Matches("^[A-Z0-9]{7}$", generated.Link!.Code);
        Assert.InRange((generated.Link.ExpiresAtUtc - generated.Link.CreatedAtUtc).TotalDays, 29.999, 30.001);

        var custom = await fixture.Service.CreateAsync("https://example.com/b", "myCode", null, "owner", CancellationToken.None);
        Assert.Equal("MYCODE", custom.Link!.Code);
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("/relative/path")]
    public async Task Non_http_destinations_are_rejected(string target)
    {
        await using var fixture = await LinkFixture.CreateAsync();
        var result = await fixture.Service.CreateAsync(target, null, null, "owner", CancellationToken.None);
        Assert.Equal(LinkResultStatus.InvalidUrl, result.Status);
    }

    [Fact]
    public async Task Expiration_at_or_before_creation_is_rejected_and_custom_code_collision_is_reported()
    {
        await using var fixture = await LinkFixture.CreateAsync();
        var past = DateTimeOffset.UtcNow.AddMinutes(-1);
        Assert.Equal(LinkResultStatus.InvalidExpiration,
            (await fixture.Service.CreateAsync("https://example.com", null, past, "owner", CancellationToken.None)).Status);
        Assert.Equal(LinkResultStatus.Success,
            (await fixture.Service.CreateAsync("https://example.com/one", "taken", null, "owner", CancellationToken.None)).Status);
        Assert.Equal(LinkResultStatus.CodeConflict,
            (await fixture.Service.CreateAsync("https://example.com/two", "TAKEN", null, "owner", CancellationToken.None)).Status);
    }

    private sealed class LinkFixture : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly AppDbContext _db;
        public LinkService Service { get; }

        private LinkFixture(SqliteConnection connection, AppDbContext db, LinkService service)
        {
            _connection = connection;
            _db = db;
            Service = service;
        }

        public static async Task<LinkFixture> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options);
            await db.Database.EnsureCreatedAsync();
            db.Users.Add(new AppUser { Id = "owner", UserName = "owner@example.com", Email = "owner@example.com" });
            await db.SaveChangesAsync();
            return new LinkFixture(connection, db, new LinkService(new LinkRepository(db), TimeProvider.System));
        }

        public async ValueTask DisposeAsync()
        {
            await _db.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }
}
