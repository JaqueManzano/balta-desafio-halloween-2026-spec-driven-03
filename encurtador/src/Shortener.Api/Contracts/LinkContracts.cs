namespace Shortener.Api.Contracts;

public sealed record CreateLinkRequest(string TargetUrl, string? Code = null, DateTimeOffset? ExpiresAtUtc = null);

public sealed record LinkSummary(
    string Code,
    string ShortUrl,
    string TargetUrl,
    DateTime CreatedAtUtc,
    DateTime ExpiresAtUtc,
    long AccessCount);
