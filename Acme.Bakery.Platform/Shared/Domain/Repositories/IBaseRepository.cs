namespace Acme.Bakery.Platform.Shared.Domain.Repositories;

// Contrato genérico de repositorio (se define en Domain y se implementa en Infrastructure).
// Sirve para cualquier entidad: Product, Order, etc.
public interface IBaseRepository<TEntity>
{
    // Marca una entidad nueva para insertarla (se guarda con UnitOfWork.CompleteAsync)
    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);

    // Busca una entidad por su id; devuelve null si no existe
    Task<TEntity?> FindByIdAsync(int id, CancellationToken cancellationToken = default);

    // Marca una entidad como modificada (se guarda con UnitOfWork.CompleteAsync)
    void Update(TEntity entity);

    // Marca una entidad para eliminarla (se guarda con UnitOfWork.CompleteAsync)
    void Remove(TEntity entity);

    // Devuelve todas las entidades de la tabla
    Task<IEnumerable<TEntity>> ListAsync(CancellationToken cancellationToken = default);
}
