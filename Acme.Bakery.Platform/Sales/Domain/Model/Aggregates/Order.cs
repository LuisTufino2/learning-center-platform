using Acme.Bakery.Platform.Sales.Domain.Model.ValueObjects;
using Acme.Bakery.Platform.Shared.Domain.Model.Entities;

namespace Acme.Bakery.Platform.Sales.Domain.Model.Aggregates;

// Aggregate root Order: pedido de un cliente para recoger un producto en la panadería.
// Solo guarda el ProductId (no un objeto Product) porque Product pertenece a otro bounded context.
public class Order : IAuditableEntity
{
    // Constructor vacío privado: solo lo usa EF Core para leer filas de la base de datos
    private Order()
    {
        CustomerName = string.Empty;
    }

    // Crea un pedido nuevo: valida cliente y cantidad, calcula el total y lo deja en PENDING
    public Order(string customerName, int productId, int quantity, decimal unitPrice, DateTime pickupDate)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(customerName);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);

        CustomerName = customerName.Trim();
        ProductId = productId;
        Quantity = quantity;
        UnitPrice = unitPrice;
        TotalAmount = unitPrice * quantity; // regla: el total lo calcula el sistema, el cliente no lo envía
        PickupDate = pickupDate;
        Status = EOrderStatus.Pending;      // regla: todo pedido nuevo empieza en PENDING
    }

    public int Id { get; private set; }                  // lo genera MySQL (autoincremental)
    public string CustomerName { get; private set; }     // nombre del cliente
    public int ProductId { get; private set; }           // id del producto (de Inventory)
    public int Quantity { get; private set; }            // unidades pedidas
    public decimal UnitPrice { get; private set; }       // precio al momento del pedido (viene de Inventory)
    public decimal TotalAmount { get; private set; }     // UnitPrice × Quantity
    public DateTime PickupDate { get; private set; }     // fecha de recojo
    public EOrderStatus Status { get; private set; }     // Pending o Delivered

    public DateTime? CreatedAt { get; set; }             // auditoría (la llena el interceptor)
    public DateTime? UpdatedAt { get; set; }             // auditoría (la llena el interceptor)
}
