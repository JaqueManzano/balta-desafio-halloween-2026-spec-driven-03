using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shortener.Api.Application;
using Shortener.Api.Data;
using Shortener.Api.Data.Entities;

namespace Shortener.UnitTests.Application;

public sealed class AccountServiceTests
{
    [Fact]
    public async Task Password_shorter_than_eight_characters_is_rejected()
    {
        await using var fixture = await AccountFixture.CreateAsync();
        var result = await fixture.Service.RegisterAsync(
            new Shortener.Api.Contracts.AccountCredentials("person@example.com", "short"),
            CancellationToken.None);
        Assert.Equal(AccountResultStatus.InvalidInput, result.Status);
    }

    [Fact]
    public async Task Incorrect_login_returns_generic_unauthorized_result()
    {
        await using var fixture = await AccountFixture.CreateAsync();
        await fixture.Service.RegisterAsync(
            new Shortener.Api.Contracts.AccountCredentials("person@example.com", "ValidPassword"),
            CancellationToken.None);
        var result = await fixture.Service.LoginAsync(
            new Shortener.Api.Contracts.AccountCredentials("unknown@example.com", "wrong"),
            CancellationToken.None);
        Assert.Equal(AccountResultStatus.Unauthorized, result.Status);
    }

    private sealed class AccountFixture : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly ServiceProvider _provider;
        private readonly AsyncServiceScope _scope;
        public AccountService Service { get; }

        private AccountFixture(SqliteConnection connection, ServiceProvider provider, AsyncServiceScope scope, AccountService service)
        {
            _connection = connection;
            _provider = provider;
            _scope = scope;
            Service = service;
        }

        public static async Task<AccountFixture> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddDbContext<AppDbContext>(options => options.UseSqlite(connection));
            services.AddIdentityCore<AppUser>(options =>
                {
                    options.User.RequireUniqueEmail = true;
                    options.Password.RequiredLength = 8;
                    options.Password.RequireDigit = false;
                    options.Password.RequireLowercase = false;
                    options.Password.RequireUppercase = false;
                    options.Password.RequireNonAlphanumeric = false;
                })
                .AddEntityFrameworkStores<AppDbContext>();
            services.AddSingleton(TimeProvider.System);
            services.AddScoped<TokenService>();
            services.AddScoped<AccountService>();
            var provider = services.BuildServiceProvider();
            var scope = provider.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreatedAsync();
            return new AccountFixture(connection, provider, scope, scope.ServiceProvider.GetRequiredService<AccountService>());
        }

        public async ValueTask DisposeAsync()
        {
            await _scope.DisposeAsync();
            await _provider.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }
}
