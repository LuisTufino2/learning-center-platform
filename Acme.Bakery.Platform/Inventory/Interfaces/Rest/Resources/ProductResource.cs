namespace Acme.Bakery.Platform.Inventory.Interfaces.Rest.Resources;

// JSON de salida de un producto (lo que devuelve la API)
public record ProductResource(
    int Id,
    string Name,
    decimal Price,
    int Stock,
    string Category,      // en mayúsculas: "BREAD", "CAKE", ...
    DateTime? CreatedAt,
    DateTime? UpdatedAt);
