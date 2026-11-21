using Acme.Bakery.Platform.Inventory.Domain.Model.Aggregates;
using Acme.Bakery.Platform.Inventory.Domain.Repositories;
using Acme.Bakery.Platform.Shared.Infrastructure.Persistence.EFC.Configuration;
using Acme.Bakery.Platform.Shared.Infrastructure.Persistence.EFC.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Acme.Bakery.Platform.Inventory.Infrastructure.Persistence.EFC.Repositories;

// Repositorio de productos con EF Core: hereda lo básico de BaseRepository y agrega lo propio.
public class ProductRepository(AppDbContext context) : BaseRepository<Product>(context), IProductRepository
{
    // Pregunta a MySQL si hay algún producto con ese nombre (devuelve true/false)
    public async Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return await Context.Set<Product>().AnyAsync(p => p.Name == name, cancellationToken);
    }
}
