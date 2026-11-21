using Acme.Bakery.Platform.Inventory.Application.CommandServices;
using Acme.Bakery.Platform.Inventory.Application.QueryServices;
using Acme.Bakery.Platform.Inventory.Domain.Model.Commands;
using Acme.Bakery.Platform.Inventory.Domain.Model.Queries;
using Acme.Bakery.Platform.Inventory.Interfaces.Acl;

namespace Acme.Bakery.Platform.Inventory.Application.Acl;

// Implementación de la fachada del ACL: traduce datos primitivos a commands/queries de Inventory.
public class InventoryContextFacade(
    IProductCommandService productCommandService,
    IProductQueryService productQueryService) : IInventoryContextFacade
{
    // Busca el producto y devuelve solo su precio (o null si no existe)
    public async Task<decimal?> FetchProductPriceByIdAsync(int productId, CancellationToken cancellationToken)
    {
        var product = await productQueryService.Handle(new GetProductByIdQuery(productId), cancellationToken);
        return product?.Price;
    }

    // Crea el command de descontar stock y devuelve solo true/false
    public async Task<bool> DecreaseProductStockAsync(int productId, int quantity,
        CancellationToken cancellationToken)
    {
        var result = await productCommandService.Handle(
            new DecreaseProductStockCommand(productId, quantity), cancellationToken);
        return result.IsSuccess;
    }
}
