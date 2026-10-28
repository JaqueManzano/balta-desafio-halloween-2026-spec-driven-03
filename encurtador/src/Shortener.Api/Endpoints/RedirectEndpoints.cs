using Shortener.Api.Application;
using Shortener.Api.Data.Repositories;

namespace Shortener.Api.Endpoints;

public static class RedirectEndpoints
{
    public static IEndpointRouteBuilder MapRedirectEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/{code}", RedirectAsync).AllowAnonymous();
        return endpoints;
    }

    private static async Task<IResult> RedirectAsync(
        string code,
        LinkService service,
        CancellationToken cancellationToken)
    {
        var result = await service.RedirectAsync(code, cancellationToken);
        return result.Status switch
        {
            RedirectStatus.Redirected when result.TargetUrl is not null => Results.Redirect(result.TargetUrl),
            RedirectStatus.NotFound => Results.NotFound(),
            RedirectStatus.Expired => Results.StatusCode(StatusCodes.Status410Gone),
            _ => throw new InvalidOperationException("Resultado de redirecionamento não reconhecido.")
        };
    }
}
