namespace Shortener.Api.Data.Entities;

public sealed class AccountToken
{
    public required string TokenHash { get; set; }
    public required string UserId { get; set; }
    public required string Type { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
    public AppUser? User { get; set; }
}
