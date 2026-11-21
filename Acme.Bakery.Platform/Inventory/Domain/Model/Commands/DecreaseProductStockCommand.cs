namespace Acme.Bakery.Platform.Inventory.Domain.Model.Commands;

// Orden para descontar stock de un producto. No tiene endpoint propio:
// la crea la fachada del ACL cuando Sales registra un pedido.
public record DecreaseProductStockCommand(int ProductId, int Quantity);
