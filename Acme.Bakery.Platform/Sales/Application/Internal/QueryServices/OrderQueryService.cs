using Acme.Bakery.Platform.Sales.Application.QueryServices;
using Acme.Bakery.Platform.Sales.Domain.Model.Aggregates;
using Acme.Bakery.Platform.Sales.Domain.Model.Queries;
using Acme.Bakery.Platform.Sales.Domain.Repositories;

namespace Acme.Bakery.Platform.Sales.Application.Internal.QueryServices;

// Implementación de las consultas de pedidos (solo lectura, nunca guarda).
public class OrderQueryService(IOrderRepository orderRepository) : IOrderQueryService
{
    // Busca un pedido por id; devuelve null si no existe (el controller responde 404)
    public async Task<Order?> Handle(GetOrderByIdQuery query, CancellationToken cancellationToken)
    {
        return await orderRepository.FindByIdAsync(query.OrderId, cancellationToken);
    }

    // Lista todos los pedidos
    public async Task<IEnumerable<Order>> Handle(GetAllOrdersQuery query, CancellationToken cancellationToken)
    {
        return await orderRepository.ListAsync(cancellationToken);
    }
}
