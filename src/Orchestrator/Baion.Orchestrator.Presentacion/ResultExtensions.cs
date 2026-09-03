using Baion.Orchestrator.Models.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Baion.Orchestrator.Presentacion;

/// <summary>Traduce el <c>Result</c> de la capa de aplicación a una respuesta HTTP.</summary>
internal static class ResultExtensions
{
    public static IActionResult ToActionResult<TValue>(this Result<TValue> result) => result switch
    {
        { IsSuccess: true, Value: not null } => new OkObjectResult(result.Value),
        { Error: not null } => Problem(result.Error),
        _ => new StatusCodeResult(StatusCodes.Status500InternalServerError)
    };

    public static IActionResult ToActionResult(this Result result) => result switch
    {
        { IsSuccess: true } => new NoContentResult(),
        { Error: not null } => Problem(result.Error),
        _ => new StatusCodeResult(StatusCodes.Status500InternalServerError)
    };

    private static IActionResult Problem(Error error)
    {
        var status = ToStatusCode(error.Kind);
        return new ObjectResult(new ProblemDetails { Title = error.Code, Detail = error.Message, Status = status }) { StatusCode = status };
    }

    private static int ToStatusCode(ErrorKind kind) => kind switch
    {
        ErrorKind.Validation => StatusCodes.Status400BadRequest,
        ErrorKind.Unauthorized => StatusCodes.Status401Unauthorized,
        ErrorKind.Forbidden => StatusCodes.Status403Forbidden,
        ErrorKind.NotFound => StatusCodes.Status404NotFound,
        ErrorKind.Conflict => StatusCodes.Status409Conflict,
        _ => StatusCodes.Status500InternalServerError
    };
}
