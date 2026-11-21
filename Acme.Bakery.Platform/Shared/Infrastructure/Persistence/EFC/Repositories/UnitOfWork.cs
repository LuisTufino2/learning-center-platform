using Acme.Bakery.Platform.Shared.Domain.Repositories;
using Acme.Bakery.Platform.Shared.Infrastructure.Persistence.EFC.Configuration;

namespace Acme.Bakery.Platform.Shared.Infrastructure.Persistence.EFC.Repositories;

// Implementación del Unit of Work con EF Core.
public class UnitOfWork(AppDbContext context) : IUnitOfWork
{
    // Envía a MySQL todos los cambios pendientes en una sola transacción
    public async Task CompleteAsync(CancellationToken cancellationToken = default)
    {
        await context.SaveChangesAsync(cancellationToken);
    }
}
