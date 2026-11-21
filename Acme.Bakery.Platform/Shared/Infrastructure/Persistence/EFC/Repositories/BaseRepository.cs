using Acme.Bakery.Platform.Shared.Domain.Repositories;
using Acme.Bakery.Platform.Shared.Infrastructure.Persistence.EFC.Configuration;
using Microsoft.EntityFrameworkCore;

namespace Acme.Bakery.Platform.Shared.Infrastructure.Persistence.EFC.Repositories;

// Implementación genérica de IBaseRepository con EF Core.
// Es abstracta: los repositorios concretos (ProductRepository, OrderRepository) heredan de ella.
public abstract class BaseRepository<TEntity>(AppDbContext context) : IBaseRepository<TEntity>
    where TEntity : class
{
    // Contexto de base de datos; protected para que los hijos también lo usen
    protected readonly AppDbContext Context = context;

    // Agrega la entidad al contexto como "Added" (todavía no la guarda en MySQL)
    public async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        await Context.Set<TEntity>().AddAsync(entity, cancellationToken);
    }

    // Busca por clave primaria; devuelve null si no existe
    public async Task<TEntity?> FindByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await Context.Set<TEntity>().FindAsync([id], cancellationToken);
    }

    // Marca la entidad como "Modified" (todavía no la guarda en MySQL)
    public void Update(TEntity entity)
    {
        Context.Set<TEntity>().Update(entity);
    }

    // Marca la entidad como "Deleted" (todavía no la borra de MySQL)
    public void Remove(TEntity entity)
    {
        Context.Set<TEntity>().Remove(entity);
    }

    // Trae todos los registros de la tabla
    public async Task<IEnumerable<TEntity>> ListAsync(CancellationToken cancellationToken = default)
    {
        return await Context.Set<TEntity>().ToListAsync(cancellationToken);
    }
}
