using Acme.Bakery.Platform.Shared.Domain.Model.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Acme.Bakery.Platform.Shared.Infrastructure.Persistence.EFC.Interceptors;

// Interceptor de EF Core: llena CreatedAt / UpdatedAt justo antes de guardar los cambios.
public class AuditableEntityInterceptor : SaveChangesInterceptor
{
    // Se ejecuta antes de SaveChanges() (versión síncrona)
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        UpdateAuditFields(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    // Se ejecuta antes de SaveChangesAsync() (versión asíncrona, la que usa UnitOfWork)
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
        InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        UpdateAuditFields(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    // Recorre las entidades auditables: a las nuevas les pone CreatedAt y UpdatedAt,
    // y a las modificadas solo les actualiza UpdatedAt
    private static void UpdateAuditFields(DbContext? context)
    {
        if (context is null) return;
        var now = DateTime.UtcNow; // misma hora (UTC) para todo lo que se guarda junto

        foreach (var entry in context.ChangeTracker.Entries<IAuditableEntity>())
        {
            if (entry.State == EntityState.Added)
                entry.Entity.CreatedAt = now;

            if (entry.State is EntityState.Added or EntityState.Modified)
                entry.Entity.UpdatedAt = now;
        }
    }
}
