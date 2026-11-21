using Acme.Bakery.Platform.Sales.Domain.Model.Aggregates;
using Acme.Bakery.Platform.Sales.Interfaces.Rest.Resources;

namespace Acme.Bakery.Platform.Sales.Interfaces.Rest.Transform;

// Traductor: entidad del dominio (Order) → JSON de salida (Resource)
public static class OrderResourceFromEntityAssembler
{
    // Convierte un Order en OrderResource (el estado sale en mayúsculas)
    public static OrderResource ToResourceFromEntity(Order entity)
    {
        return new OrderResource(
            entity.Id,
            entity.CustomerName,
            entity.ProductId,
            entity.Quantity,
            entity.UnitPrice,
            entity.TotalAmount,
            entity.PickupDate,
            entity.Status.ToString().ToUpperInvariant(),
            entity.CreatedAt,
            entity.UpdatedAt);
    }
}
