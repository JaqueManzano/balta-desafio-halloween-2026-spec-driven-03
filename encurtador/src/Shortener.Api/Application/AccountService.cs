using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Shortener.Api.Contracts;
using Shortener.Api.Data.Entities;

namespace Shortener.Api.Application;

public enum AccountResultStatus
{
    Success,
    InvalidInput,
    Conflict,
    Unauthorized
}

public sealed record AccountResult(AccountResultStatus Status, TokenResponse? Tokens = null);

public sealed class AccountService(UserManager<AppUser> users, TokenService tokens)
{
    public async Task<AccountResult> RegisterAsync(AccountCredentials credentials, CancellationToken cancellationToken)
    {
        if (!new EmailAddressAttribute().IsValid(credentials.Email))
        {
            return new AccountResult(AccountResultStatus.InvalidInput);
        }

        if (credentials.Password.Length < 8)
        {
            return new AccountResult(AccountResultStatus.InvalidInput);
        }

        var user = new AppUser { UserName = credentials.Email, Email = credentials.Email };
        var result = await users.CreateAsync(user, credentials.Password);
        if (result.Succeeded)
        {
            return new AccountResult(AccountResultStatus.Success);
        }

        if (result.Errors.Any(error => error.Code is "DuplicateEmail" or "DuplicateUserName"))
        {
            return new AccountResult(AccountResultStatus.Conflict);
        }

        return new AccountResult(AccountResultStatus.InvalidInput);
    }

    public async Task<AccountResult> LoginAsync(AccountCredentials credentials, CancellationToken cancellationToken)
    {
        var user = await users.FindByEmailAsync(credentials.Email);
        if (user is null || !await users.CheckPasswordAsync(user, credentials.Password))
        {
            return new AccountResult(AccountResultStatus.Unauthorized);
        }

        return new AccountResult(AccountResultStatus.Success,
            await tokens.CreateTokensAsync(user, cancellationToken));
    }

    public async Task<TokenResponse?> RefreshAsync(string refreshToken, CancellationToken cancellationToken) =>
        await tokens.RefreshAsync(refreshToken, cancellationToken);
}
