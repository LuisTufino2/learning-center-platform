using Acme.Bakery.Platform.Inventory.Interfaces.Acl;

namespace Acme.Bakery.Platform.Sales.Application.Internal.OutboundServices;

// Servicio de salida (ACL del lado de Sales): el ÚNICO lugar de Sales que conoce a Inventory.
// Si Inventory cambia su fachada, solo hay que modificar esta clase.
public class ExternalInventoryService(IInventoryContextFacade inventoryContextFacade)
{
    // Pide a Inventory el precio del producto (null si el producto no existe)
    public Task<decimal?> FetchProductPriceAsync(int productId, CancellationToken cancellationToken)
    {
        return inventoryContextFacade.FetchProductPriceByIdAsync(productId, cancellationToken);
    }

    // Pide a Inventory que descuente stock (true si se pudo, false si no alcanza)
    public Task<bool> ReserveStockAsync(int productId, int quantity, CancellationToken cancellationToken)
    {
        return inventoryContextFacade.DecreaseProductStockAsync(productId, quantity, cancellationToken);
    }
}
