namespace Acme.Bakery.Platform.Inventory.Interfaces.Acl;

// Contrato público que Inventory expone a OTROS bounded contexts (Anti-Corruption Layer).
// Solo entran y salen datos primitivos: los demás contextos nunca ven el agregado Product.
public interface IInventoryContextFacade
{
    // Devuelve el precio unitario del producto, o null si el producto no existe
    Task<decimal?> FetchProductPriceByIdAsync(int productId, CancellationToken cancellationToken);

    // Descuenta stock; devuelve true si se pudo, false si el producto no existe o no hay stock
    Task<bool> DecreaseProductStockAsync(int productId, int quantity, CancellationToken cancellationToken);
}
