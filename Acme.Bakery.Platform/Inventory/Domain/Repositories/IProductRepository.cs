using Acme.Bakery.Platform.Inventory.Domain.Model.Aggregates;
using Acme.Bakery.Platform.Shared.Domain.Repositories;

namespace Acme.Bakery.Platform.Inventory.Domain.Repositories;

// Contrato del repositorio de productos: hereda Add, FindById, Update, Remove y List.
public interface IProductRepository : IBaseRepository<Product>
{
    // Indica si ya existe un producto con ese nombre (regla "nombre único" → 409)
    Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken = default);
}
