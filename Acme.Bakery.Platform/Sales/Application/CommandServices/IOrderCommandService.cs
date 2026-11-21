using Acme.Bakery.Platform.Sales.Domain.Model.Aggregates;
using Acme.Bakery.Platform.Sales.Domain.Model.Commands;
using Acme.Bakery.Platform.Shared.Application.Model;

namespace Acme.Bakery.Platform.Sales.Application.CommandServices;

// Contrato de las operaciones que MODIFICAN pedidos (CQRS: lado de comandos).
public interface IOrderCommandService
{
    // Registra un pedido: valida, consulta precio, descuenta stock y guarda todo junto
    Task<Result<Order>> Handle(CreateOrderCommand command, CancellationToken cancellationToken);
}
