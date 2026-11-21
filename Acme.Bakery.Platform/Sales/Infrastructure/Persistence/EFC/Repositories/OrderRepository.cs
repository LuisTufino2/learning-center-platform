using Acme.Bakery.Platform.Sales.Domain.Model.Aggregates;
using Acme.Bakery.Platform.Sales.Domain.Repositories;
using Acme.Bakery.Platform.Shared.Infrastructure.Persistence.EFC.Configuration;
using Acme.Bakery.Platform.Shared.Infrastructure.Persistence.EFC.Repositories;

namespace Acme.Bakery.Platform.Sales.Infrastructure.Persistence.EFC.Repositories;

// Repositorio de pedidos con EF Core: todo lo hereda de BaseRepository (no necesita métodos extra)
public class OrderRepository(AppDbContext context) : BaseRepository<Order>(context), IOrderRepository;
