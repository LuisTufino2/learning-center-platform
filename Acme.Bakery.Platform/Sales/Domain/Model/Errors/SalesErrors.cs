using Acme.Bakery.Platform.Shared.Domain.Model.Errors;

namespace Acme.Bakery.Platform.Sales.Domain.Model.Errors;

// Catálogo de errores del bounded context Sales (una regla del enunciado = un error).
public static class SalesErrors
{
    // 400: el nombre del cliente viene vacío
    public static readonly Error CustomerNameRequired =
        Error.Validation("Sales.CustomerNameRequired", "Customer name is required.");

    // 400: la cantidad es menor que 1
    public static readonly Error InvalidQuantity =
        Error.Validation("Sales.InvalidQuantity", "Quantity must be at least 1.");

    // 400: la fecha de recojo es anterior a hoy
    public static readonly Error PickupDateInPast =
        Error.Validation("Sales.PickupDateInPast", "Pickup date cannot be in the past.");

    // 404: el producto pedido no existe en Inventory
    public static readonly Error ProductNotFound =
        Error.NotFound("Sales.ProductNotFound", "The requested product does not exist.");

    // 409: no hay stock suficiente para la cantidad pedida
    public static readonly Error InsufficientStock =
        Error.Conflict("Sales.InsufficientStock", "There is not enough stock for the requested quantity.");
}
