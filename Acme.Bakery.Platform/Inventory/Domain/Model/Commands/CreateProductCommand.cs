namespace Acme.Bakery.Platform.Inventory.Domain.Model.Commands;

// Orden para registrar un producto nuevo (solo los datos que envía el cliente).
// Category va como string para que el servicio la valide y responda 400 si es inválida.
public record CreateProductCommand(string Name, decimal Price, int Stock, string Category);
