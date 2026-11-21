using Acme.Bakery.Platform.Shared.Domain.Model.Errors;

namespace Acme.Bakery.Platform.Inventory.Domain.Model.Errors;

// Catálogo de errores del bounded context Inventory (una regla del enunciado = un error).
public static class InventoryErrors
{
    // 400: el nombre viene vacío
    public static readonly Error NameRequired =
        Error.Validation("Inventory.NameRequired", "Product name is required.");

    // 400: el precio es 0 o negativo
    public static readonly Error InvalidPrice =
        Error.Validation("Inventory.InvalidPrice", "Price must be greater than 0.");

    // 400: el stock es negativo
    public static readonly Error InvalidStock =
        Error.Validation("Inventory.InvalidStock", "Stock cannot be negative.");

    // 400: la categoría no es BREAD, CAKE, PASTRY ni COOKIE
    public static readonly Error InvalidCategory =
        Error.Validation("Inventory.InvalidCategory", "Category must be one of: BREAD, CAKE, PASTRY, COOKIE.");

    // 409: ya existe un producto con ese nombre
    public static readonly Error NameAlreadyExists =
        Error.Conflict("Inventory.NameAlreadyExists", "A product with the same name already exists.");

    // 404: no existe un producto con ese id
    public static readonly Error ProductNotFound =
        Error.NotFound("Inventory.ProductNotFound", "The specified product was not found.");

    // 409: no hay stock suficiente para la cantidad pedida
    public static readonly Error InsufficientStock =
        Error.Conflict("Inventory.InsufficientStock", "There is not enough stock for the requested quantity.");
}
