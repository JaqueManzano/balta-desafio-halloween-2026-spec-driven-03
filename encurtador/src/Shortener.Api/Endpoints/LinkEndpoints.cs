using System.Security.Claims;
using Shortener.Api.Application;
using Shortener.Api.Contracts;
using Shortener.Api.Data.Entities;
using Shortener.Api.Extensions;

namespace Shortener.Api.Endpoints;

public static class LinkEndpoints
{
    public static IEndpointRouteBuilder MapLinkEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/links").RequireAuthorization();
        group.MapPost("", CreateAsync);
        group.MapGet("", ListAsync);
        group.MapGet("/{code}/stats", GetStatsAsync);
        return endpoints;
    }

    private static async Task<IResult> CreateAsync(
        CreateLinkRequest? request,
        ClaimsPrincipal user,
        LinkService service,
        HttpRequest httpRequest,
        CancellationToken cancellationToken)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.TargetUrl))
        {
            return ValidationProblemExtensions.InvalidInput("targetUrl", "Informe uma URL de destino.");
        }

        var ownerId = user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("O usuário autenticado não possui identificador.");
        var result = await service.CreateAsync(
            request.TargetUrl,
            request.Code,
            request.ExpiresAtUtc,
            ownerId,
            cancellationToken);

        if (result.Status != LinkResultStatus.Success || result.Link is null)
        {
            return result.Status switch
            {
                LinkResultStatus.InvalidUrl => ValidationProblemExtensions.InvalidInput("targetUrl", "Informe uma URL absoluta HTTP ou HTTPS válida."),
                LinkResultStatus.InvalidCode => ValidationProblemExtensions.InvalidInput("code", "O código deve conter de 4 a 20 caracteres alfanuméricos."),
                LinkResultStatus.InvalidExpiration => ValidationProblemExtensions.InvalidInput("expiresAtUtc", "A expiração deve ser posterior ao instante atual."),
                LinkResultStatus.CodeConflict => Results.Conflict(new { message = "O código personalizado não está disponível." }),
                _ => throw new InvalidOperationException("Resultado de criação de link não reconhecido.")
            };
        }

        return Results.Created($"/{result.Link.Code}", ToSummary(result.Link, httpRequest));
    }

    private static async Task<IResult> ListAsync(
        ClaimsPrincipal user,
        LinkService service,
        HttpRequest httpRequest,
        CancellationToken cancellationToken)
    {
        var ownerId = user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("O usuário autenticado não possui identificador.");
        var links = await service.ListAsync(ownerId, cancellationToken);
        return Results.Ok(links.Select(link => ToSummary(link, httpRequest)));
    }

    private static async Task<IResult> GetStatsAsync(
        string code,
        ClaimsPrincipal user,
        LinkService service,
        HttpRequest httpRequest,
        CancellationToken cancellationToken)
    {
        var ownerId = user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("O usuário autenticado não possui identificador.");
        var link = await service.GetStatsAsync(code, ownerId, cancellationToken);
        return link is null ? Results.NotFound() : Results.Ok(ToSummary(link, httpRequest));
    }

    private static LinkSummary ToSummary(Link link, HttpRequest request) =>
        new(link.Code, $"{request.Scheme}://{request.Host}/{link.Code}", link.TargetUrl,
            DateTime.SpecifyKind(link.CreatedAtUtc, DateTimeKind.Utc),
            DateTime.SpecifyKind(link.ExpiresAtUtc, DateTimeKind.Utc), link.AccessCount);
}
