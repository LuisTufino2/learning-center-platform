using Acme.Bakery.Platform.Shared.Domain.Model.Errors;
using Microsoft.AspNetCore.Mvc;

namespace Acme.Bakery.Platform.Shared.Interfaces.Rest.Transform;

// Convierte un Error del dominio en una respuesta HTTP con formato ProblemDetails (RFC 9457).
public static class ErrorActionResultAssembler
{
    // Elige el código HTTP según el tipo de error y arma la respuesta ProblemDetails
    public static IActionResult ToActionResultFromError(ControllerBase controller, Error error)
    {
        var statusCode = error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError // por si aparece un tipo no contemplado
        };

        return controller.Problem(title: error.Code, detail: error.Message, statusCode: statusCode);
    }
}
