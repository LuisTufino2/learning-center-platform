namespace Acme.Bakery.Platform.Sales.Domain.Model.Commands;

// Orden para registrar un pedido (solo los datos que envía el cliente;
// el precio, el total y el estado los pone el sistema)
public record CreateOrderCommand(string CustomerName, int ProductId, int Quantity, DateTime PickupDate);
