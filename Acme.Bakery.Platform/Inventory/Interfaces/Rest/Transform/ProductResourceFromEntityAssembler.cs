using Acme.Bakery.Platform.Inventory.Domain.Model.Aggregates;
using Acme.Bakery.Platform.Inventory.Interfaces.Rest.Resources;

namespace Acme.Bakery.Platform.Inventory.Interfaces.Rest.Transform;

// Traductor: entidad del dominio (Product) → JSON de salida (Resource)
public static class ProductResourceFromEntityAssembler
{
    // Convierte un Product en ProductResource (la categoría sale en mayúsculas)
    public static ProductResource ToResourceFromEntity(Product entity)
    {
        return new ProductResource(
            entity.Id,
            entity.Name,
            entity.Price,
            entity.Stock,
            entity.Category.ToString().ToUpperInvariant(),
            entity.CreatedAt,
            entity.UpdatedAt);
    }
}
