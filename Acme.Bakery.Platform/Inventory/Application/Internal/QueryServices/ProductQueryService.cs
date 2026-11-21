using Acme.Bakery.Platform.Inventory.Application.QueryServices;
using Acme.Bakery.Platform.Inventory.Domain.Model.Aggregates;
using Acme.Bakery.Platform.Inventory.Domain.Model.Queries;
using Acme.Bakery.Platform.Inventory.Domain.Repositories;

namespace Acme.Bakery.Platform.Inventory.Application.Internal.QueryServices;

// Implementación de las consultas de productos (solo lectura, nunca guarda).
public class ProductQueryService(IProductRepository productRepository) : IProductQueryService
{
    // Busca un producto por id; devuelve null si no existe (el controller responde 404)
    public async Task<Product?> Handle(GetProductByIdQuery query, CancellationToken cancellationToken)
    {
        return await productRepository.FindByIdAsync(query.ProductId, cancellationToken);
    }

    // Lista todos los productos
    public async Task<IEnumerable<Product>> Handle(GetAllProductsQuery query, CancellationToken cancellationToken)
    {
        return await productRepository.ListAsync(cancellationToken);
    }
}
