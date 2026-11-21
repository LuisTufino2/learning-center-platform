using Acme.Bakery.Platform.Sales.Domain.Model.Aggregates;
using Acme.Bakery.Platform.Sales.Domain.Model.Queries;

namespace Acme.Bakery.Platform.Sales.Application.QueryServices;

// Contrato de las operaciones que solo LEEN pedidos (CQRS: lado de consultas).
public interface IOrderQueryService
{
    // Devuelve el pedido con ese id, o null si no existe
    Task<Order?> Handle(GetOrderByIdQuery query, CancellationToken cancellationToken);

    // Devuelve todos los pedidos
    Task<IEnumerable<Order>> Handle(GetAllOrdersQuery query, CancellationToken cancellationToken);
}
