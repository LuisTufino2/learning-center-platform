namespace Acme.Bakery.Platform.Sales.Interfaces.Rest.Resources;

// JSON de entrada para POST /api/v1/orders (lo que envía el cliente)
public record CreateOrderResource(string CustomerName, int ProductId, int Quantity, DateTime PickupDate);
