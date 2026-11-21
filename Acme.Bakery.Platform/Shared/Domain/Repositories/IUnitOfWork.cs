namespace Acme.Bakery.Platform.Shared.Domain.Repositories;

// Unit of Work: confirma todos los cambios pendientes en una sola transacción.
public interface IUnitOfWork
{
    // Guarda en la base de datos todo lo que los repositorios dejaron anotado
    Task CompleteAsync(CancellationToken cancellationToken = default);
}
