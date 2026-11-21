using Acme.Bakery.Platform.Shared.Domain.Model.Errors;

namespace Acme.Bakery.Platform.Shared.Application.Model;

// Patrón Result: un servicio devuelve éxito (con un valor) o fallo (con un Error),
// en lugar de lanzar excepciones cuando se rompe una regla de negocio.
public sealed class Result<T>
{
    // Constructor privado: solo se puede crear con Success() o Failure()
    private Result(T? value, Error? error)
    {
        Value = value;
        Error = error;
    }

    // El valor devuelto cuando todo salió bien (null si falló)
    public T? Value { get; }

    // El error cuando algo salió mal (null si fue éxito)
    public Error? Error { get; }

    // true si no hay error
    public bool IsSuccess => Error is null;

    // true si hay error (lo contrario de IsSuccess)
    public bool IsFailure => !IsSuccess;

    // Crea un resultado exitoso con su valor
    public static Result<T> Success(T value) => new(value, null);

    // Crea un resultado fallido con su error
    public static Result<T> Failure(Error error) => new(default, error);
}
