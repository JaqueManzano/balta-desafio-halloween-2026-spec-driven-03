namespace Shortener.Api.Contracts;

public sealed record AccountCredentials(string Email, string Password);

public sealed record RefreshRequest(string RefreshToken);

public sealed record TokenResponse(string AccessToken, string RefreshToken, int ExpiresIn, string TokenType = "Bearer");
