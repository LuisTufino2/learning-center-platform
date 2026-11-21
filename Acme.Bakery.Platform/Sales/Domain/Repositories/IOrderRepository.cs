using Acme.Bakery.Platform.Sales.Domain.Model.Aggregates;
using Acme.Bakery.Platform.Shared.Domain.Repositories;

namespace Acme.Bakery.Platform.Sales.Domain.Repositories;

// Contrato del repositorio de pedidos: hereda Add, FindById, Update, Remove y List
// (no necesita métodos extra porque el enunciado no pide otras búsquedas)
public interface IOrderRepository : IBaseRepository<Order>;
