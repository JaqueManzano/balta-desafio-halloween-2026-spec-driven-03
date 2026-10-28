using Microsoft.AspNetCore.Mvc;

namespace Shortener.Api.Extensions;

public static class ValidationProblemExtensions
{
    public static IResult InvalidInput(string field, string message) =>
        Results.ValidationProblem(
            new Dictionary<string, string[]> { [field] = [message] },
            title: "A solicitação contém dados inválidos.",
            statusCode: StatusCodes.Status400BadRequest);
}
