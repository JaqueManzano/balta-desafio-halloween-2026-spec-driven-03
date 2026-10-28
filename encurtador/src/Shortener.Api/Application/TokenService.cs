using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Shortener.Api.Contracts;
using Shortener.Api.Data;
using Shortener.Api.Data.Entities;

namespace Shortener.Api.Application;

public sealed class TokenService(AppDbContext db, TimeProvider timeProvider)
{
    private static readonly TimeSpan AccessTokenLifetime = TimeSpan.FromHours(1);
    private static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(30);

    public async Task<TokenResponse> CreateTokensAsync(AppUser user, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var accessToken = CreateOpaqueToken();
        var refreshToken = CreateOpaqueToken();
        db.AccountTokens.AddRange(
            CreateTokenEntity(user.Id, accessToken, "access", now.Add(AccessTokenLifetime)),
            CreateTokenEntity(user.Id, refreshToken, "refresh", now.Add(RefreshTokenLifetime)));
        await db.SaveChangesAsync(cancellationToken);
        return new TokenResponse(accessToken, refreshToken, (int)AccessTokenLifetime.TotalSeconds);
    }

    public async Task<AppUser?> ValidateAccessTokenAsync(string token, CancellationToken cancellationToken)
    {
        var hash = Hash(token);
        var now = timeProvider.GetUtcNow().UtcDateTime;
        return await db.AccountTokens
            .Where(item => item.TokenHash == hash && item.Type == "access" &&
                           item.RevokedAtUtc == null && item.ExpiresAtUtc > now)
            .Select(item => item.User)
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<TokenResponse?> RefreshAsync(string refreshToken, CancellationToken cancellationToken)
    {
        var hash = Hash(refreshToken);
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var token = await db.AccountTokens
            .Include(item => item.User)
            .SingleOrDefaultAsync(item => item.TokenHash == hash && item.Type == "refresh" &&
                                          item.RevokedAtUtc == null && item.ExpiresAtUtc > now,
                cancellationToken);
        if (token?.User is null)
        {
            return null;
        }

        token.RevokedAtUtc = now;
        return await CreateTokensAsync(token.User, cancellationToken);
    }

    private static AccountToken CreateTokenEntity(string userId, string token, string type, DateTime expiresAtUtc) =>
        new()
        {
            TokenHash = Hash(token),
            UserId = userId,
            Type = type,
            ExpiresAtUtc = expiresAtUtc
        };

    private static string CreateOpaqueToken() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(48))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

    private static string Hash(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
