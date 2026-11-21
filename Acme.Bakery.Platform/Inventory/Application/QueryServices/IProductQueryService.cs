using Acme.Bakery.Platform.Inventory.Domain.Model.Aggregates;
using Acme.Bakery.Platform.Inventory.Domain.Model.Queries;

namespace Acme.Bakery.Platform.Inventory.Application.QueryServices;

// Contrato de las operaciones que solo LEEN productos (CQRS: lado de consultas).
public interface IProductQueryService
{
    // Devuelve el producto con ese id, o null si no existe
    Task<Product?> Handle(GetProductByIdQuery query, CancellationToken cancellationToken);

    // Devuelve todos los productos
    Task<IEnumerable<Product>> Handle(GetAllProductsQuery query, CancellationToken cancellationToken);
}
