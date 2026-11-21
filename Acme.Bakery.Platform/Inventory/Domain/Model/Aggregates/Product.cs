using Acme.Bakery.Platform.Inventory.Domain.Model.ValueObjects;
using Acme.Bakery.Platform.Shared.Domain.Model.Entities;

namespace Acme.Bakery.Platform.Inventory.Domain.Model.Aggregates;

// Aggregate root Product: algo que vende la panadería (pan, torta, pastel, galleta).
// Su estado solo cambia mediante sus propios métodos (setters privados).
public class Product : IAuditableEntity
{
    // Constructor vacío privado: solo lo usa EF Core para leer filas de la base de datos
    private Product()
    {
        Name = string.Empty;
    }

    // Crea un producto nuevo validando las reglas: nombre obligatorio, precio > 0 y stock >= 0
    public Product(string name, decimal price, int stock, EProductCategory category)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(price);
        ArgumentOutOfRangeException.ThrowIfNegative(stock);

        Name = name.Trim(); // quita espacios al inicio y al final
        Price = price;
        Stock = stock;
        Category = category;
    }

    public int Id { get; private set; }                         // lo genera MySQL (autoincremental)
    public string Name { get; private set; }                    // nombre único del producto
    public decimal Price { get; private set; }                  // precio unitario (decimal por ser dinero)
    public int Stock { get; private set; }                      // unidades disponibles
    public EProductCategory Category { get; private set; }      // Bread, Cake, Pastry o Cookie

    public DateTime? CreatedAt { get; set; }                    // auditoría (la llena el interceptor)
    public DateTime? UpdatedAt { get; set; }                    // auditoría (la llena el interceptor)

    // Indica si hay stock suficiente para la cantidad pedida
    public bool HasStockFor(int quantity) => quantity > 0 && Stock >= quantity;

    // Descuenta stock; lanza excepción si no alcanza (el stock nunca queda negativo)
    public void DecreaseStock(int quantity)
    {
        if (!HasStockFor(quantity))
            throw new InvalidOperationException($"Not enough stock for product {Id}.");
        Stock -= quantity;
    }
}
