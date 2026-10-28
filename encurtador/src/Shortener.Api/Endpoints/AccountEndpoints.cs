using Shortener.Api.Application;
using Shortener.Api.Contracts;
using Shortener.Api.Extensions;

namespace Shortener.Api.Endpoints;

public static class AccountEndpoints
{
    public static IEndpointRouteBuilder MapAccountEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/accounts");
        group.MapPost("", RegisterAsync).AllowAnonymous();
        group.MapPost("/login", LoginAsync).AllowAnonymous();
        group.MapPost("/refresh", RefreshAsync).AllowAnonymous();
        group.MapPost("/logout", () => Results.NoContent()).RequireAuthorization();
        return endpoints;
    }

    private static async Task<IResult> RegisterAsync(
        AccountCredentials? credentials,
        AccountService service,
        CancellationToken cancellationToken)
    {
        if (credentials is null || string.IsNullOrWhiteSpace(credentials.Email) ||
            string.IsNullOrWhiteSpace(credentials.Password))
        {
            return ValidationProblemExtensions.InvalidInput("credentials", "Informe um e-mail e uma senha válidos.");
        }

        var result = await service.RegisterAsync(credentials, cancellationToken);
        return result.Status switch
        {
            AccountResultStatus.Success => Results.Created("/api/accounts", null),
            AccountResultStatus.Conflict => Results.Conflict(new { message = "Não foi possível criar a conta com os dados informados." }),
            _ => ValidationProblemExtensions.InvalidInput("credentials", "Informe um e-mail válido e uma senha com pelo menos 8 caracteres.")
        };
    }

    private static async Task<IResult> LoginAsync(
        AccountCredentials? credentials,
        AccountService service,
        CancellationToken cancellationToken)
    {
        if (credentials is null || string.IsNullOrWhiteSpace(credentials.Email) ||
            string.IsNullOrWhiteSpace(credentials.Password))
        {
            return ValidationProblemExtensions.InvalidInput("credentials", "Informe suas credenciais.");
        }

        var result = await service.LoginAsync(credentials, cancellationToken);
        return result.Status == AccountResultStatus.Success
            ? Results.Ok(result.Tokens)
            : Results.Unauthorized();
    }

    private static async Task<IResult> RefreshAsync(
        RefreshRequest? request,
        AccountService service,
        CancellationToken cancellationToken)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return Results.Unauthorized();
        }

        var tokens = await service.RefreshAsync(request.RefreshToken, cancellationToken);
        return tokens is null ? Results.Unauthorized() : Results.Ok(tokens);
    }
}
