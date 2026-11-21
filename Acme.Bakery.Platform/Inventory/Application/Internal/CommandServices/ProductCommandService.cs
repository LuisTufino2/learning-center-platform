using Acme.Bakery.Platform.Inventory.Application.CommandServices;
using Acme.Bakery.Platform.Inventory.Domain.Model.Aggregates;
using Acme.Bakery.Platform.Inventory.Domain.Model.Commands;
using Acme.Bakery.Platform.Inventory.Domain.Model.Errors;
using Acme.Bakery.Platform.Inventory.Domain.Model.ValueObjects;
using Acme.Bakery.Platform.Inventory.Domain.Repositories;
using Acme.Bakery.Platform.Shared.Application.Model;
using Acme.Bakery.Platform.Shared.Domain.Repositories;

namespace Acme.Bakery.Platform.Inventory.Application.Internal.CommandServices;

// Implementación de los comandos de productos: aplica las reglas de negocio de Inventory.
public class ProductCommandService(IProductRepository productRepository, IUnitOfWork unitOfWork)
    : IProductCommandService
{
    // Registra un producto: valida datos (400), revisa nombre único (409), lo crea y lo guarda (201)
    public async Task<Result<Product>> Handle(CreateProductCommand command, CancellationToken cancellationToken)
    {
        // 1. Validar reglas de negocio
        if (string.IsNullOrWhiteSpace(command.Name))
            return Result<Product>.Failure(InventoryErrors.NameRequired);

        if (command.Price <= 0)
            return Result<Product>.Failure(InventoryErrors.InvalidPrice);

        if (command.Stock < 0)
            return Result<Product>.Failure(InventoryErrors.InvalidStock);

        // Categoría válida: existe en el enum (sin importar mayúsculas) y no es un número como "1"
        if (!Enum.TryParse<EProductCategory>(command.Category, true, out var category)
            || !Enum.IsDefined(category)
            || int.TryParse(command.Category, out _))
            return Result<Product>.Failure(InventoryErrors.InvalidCategory);

        if (await productRepository.ExistsByNameAsync(command.Name.Trim(), cancellationToken))
            return Result<Product>.Failure(InventoryErrors.NameAlreadyExists);

        // 2. Crear el agregado y guardarlo
        var product = new Product(command.Name, command.Price, command.Stock, category);
        await productRepository.AddAsync(product, cancellationToken);
        await unitOfWork.CompleteAsync(cancellationToken);

        return Result<Product>.Success(product);
    }

    // Descuenta stock: busca el producto (404), verifica stock (409) y lo descuenta.
    // NO llama a unitOfWork.CompleteAsync(): lo usa Sales (a través del ACL) y Sales guarda
    // el cambio de stock junto con el nuevo pedido en un solo SaveChanges.
    public async Task<Result<Product>> Handle(DecreaseProductStockCommand command,
        CancellationToken cancellationToken)
    {
        var product = await productRepository.FindByIdAsync(command.ProductId, cancellationToken);
        if (product is null)
            return Result<Product>.Failure(InventoryErrors.ProductNotFound);

        if (!product.HasStockFor(command.Quantity))
            return Result<Product>.Failure(InventoryErrors.InsufficientStock);

        product.DecreaseStock(command.Quantity);
        productRepository.Update(product); // solo anota el cambio

        return Result<Product>.Success(product);
    }
}
