using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Shortener.Api.Data.Entities;
using Shortener.Api.Data.Repositories;

namespace Shortener.Api.Application;

public enum LinkResultStatus
{
    Success,
    InvalidUrl,
    InvalidCode,
    InvalidExpiration,
    CodeConflict
}

public sealed record LinkResult(LinkResultStatus Status, Link? Link = null);

public sealed class LinkService(LinkRepository links, TimeProvider timeProvider)
{
    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    private static readonly Regex CodePattern = new("^[A-Za-z0-9]{4,20}$", RegexOptions.Compiled);

    public async Task<LinkResult> CreateAsync(
        string targetUrl,
        string? requestedCode,
        DateTimeOffset? requestedExpiration,
        string ownerId,
        CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(targetUrl, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return new LinkResult(LinkResultStatus.InvalidUrl);
        }

        if (requestedCode is not null && !CodePattern.IsMatch(requestedCode))
        {
            return new LinkResult(LinkResultStatus.InvalidCode);
        }

        var createdAtUtc = timeProvider.GetUtcNow().UtcDateTime;
        var expiresAtUtc = requestedExpiration?.UtcDateTime ?? createdAtUtc.AddDays(30);
        if (expiresAtUtc <= createdAtUtc)
        {
            return new LinkResult(LinkResultStatus.InvalidExpiration);
        }

        var customCode = requestedCode is not null;
        for (var attempt = 0; attempt < 10; attempt++)
        {
            var code = customCode ? requestedCode!.ToUpperInvariant() : GenerateCode();
            var link = new Link
            {
                Code = code,
                TargetUrl = uri.AbsoluteUri,
                CreatedAtUtc = createdAtUtc,
                ExpiresAtUtc = expiresAtUtc,
                AccessCount = 0,
                OwnerId = ownerId
            };

            if (await links.CreateAsync(link, cancellationToken))
            {
                return new LinkResult(LinkResultStatus.Success, link);
            }

            if (customCode)
            {
                return new LinkResult(LinkResultStatus.CodeConflict);
            }
        }

        throw new InvalidOperationException("Não foi possível gerar um código único após várias tentativas.");
    }

    public Task<RedirectResult> RedirectAsync(string code, CancellationToken cancellationToken) =>
        links.RedirectAndCountAsync(code.ToUpperInvariant(), timeProvider.GetUtcNow().UtcDateTime, cancellationToken);

    public Task<List<Link>> ListAsync(string ownerId, CancellationToken cancellationToken) =>
        links.ListByOwnerAsync(ownerId, cancellationToken);

    public Task<Link?> GetStatsAsync(string code, string ownerId, CancellationToken cancellationToken) =>
        links.FindByOwnerAsync(code.ToUpperInvariant(), ownerId, cancellationToken);

    private static string GenerateCode()
    {
        Span<char> code = stackalloc char[7];
        for (var index = 0; index < code.Length; index++)
        {
            code[index] = Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)];
        }

        return new string(code);
    }
}
