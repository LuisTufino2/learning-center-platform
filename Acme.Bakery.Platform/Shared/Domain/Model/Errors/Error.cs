namespace Acme.Bakery.Platform.Shared.Domain.Model.Errors;

// Tipo de error. La capa REST convierte cada tipo en un código HTTP.
public enum ErrorType
{
    Validation, // 400 Bad Request: el dato enviado es inválido
    NotFound,   // 404 Not Found: lo que se busca no existe
    Conflict    // 409 Conflict: el dato es válido pero choca con el estado actual
}

// Error de dominio (record inmutable) con código, mensaje y tipo.
// Cada bounded context declara su propio catálogo de errores usando estos métodos.
public record Error(string Code, string Message, ErrorType Type)
{
    // Crea un error de validación (400)
    public static Error Validation(string code, string message) => new(code, message, ErrorType.Validation);

    // Crea un error de "no encontrado" (404)
    public static Error NotFound(string code, string message) => new(code, message, ErrorType.NotFound);

    // Crea un error de conflicto (409)
    public static Error Conflict(string code, string message) => new(code, message, ErrorType.Conflict);
}
