namespace Acme.Bakery.Platform.Inventory.Interfaces.Rest.Resources;

// JSON de entrada para POST /api/v1/products (lo que envía el cliente)
public record CreateProductResource(string Name, decimal Price, int Stock, string Category);
