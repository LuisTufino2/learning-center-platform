using Acme.Bakery.Platform.Inventory.Domain.Model.Aggregates;
using Acme.Bakery.Platform.Inventory.Domain.Model.Commands;
using Acme.Bakery.Platform.Shared.Application.Model;

namespace Acme.Bakery.Platform.Inventory.Application.CommandServices;

// Contrato de las operaciones que MODIFICAN productos (CQRS: lado de comandos).
public interface IProductCommandService
{
    // Registra un producto nuevo: valida, verifica nombre único y guarda
    Task<Result<Product>> Handle(CreateProductCommand command, CancellationToken cancellationToken);

    // Descuenta stock de un producto (no guarda; lo confirma Sales junto con el pedido)
    Task<Result<Product>> Handle(DecreaseProductStockCommand command, CancellationToken cancellationToken);
}
