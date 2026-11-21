namespace Acme.Bakery.Platform.Sales.Interfaces.Rest.Resources;

// JSON de salida de un pedido (lo que devuelve la API)
public record OrderResource(
    int Id,
    string CustomerName,
    int ProductId,
    int Quantity,
    decimal UnitPrice,
    decimal TotalAmount,
    DateTime PickupDate,
    string Status,        // en mayúsculas: "PENDING", "DELIVERED"
    DateTime? CreatedAt,
    DateTime? UpdatedAt);
