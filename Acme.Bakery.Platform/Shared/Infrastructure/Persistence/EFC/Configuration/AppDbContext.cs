using Acme.Bakery.Platform.Inventory.Infrastructure.Persistence.EFC.Configuration.Extensions;
using Acme.Bakery.Platform.Sales.Infrastructure.Persistence.EFC.Configuration.Extensions;
using Acme.Bakery.Platform.Shared.Infrastructure.Persistence.EFC.Interceptors;
using Microsoft.EntityFrameworkCore;

namespace Acme.Bakery.Platform.Shared.Infrastructure.Persistence.EFC.Configuration;

// DbContext único para toda la aplicación (la "puerta" a MySQL).
// Cada bounded context aporta la configuración de sus tablas con un método de extensión.
public class AppDbContext(DbContextOptions options) : DbContext(options)
{
    // Se ejecuta al crear el contexto: registra el interceptor de auditoría
    protected override void OnConfiguring(DbContextOptionsBuilder builder)
    {
        builder.AddInterceptors(new AuditableEntityInterceptor());
        base.OnConfiguring(builder);
    }

    // Se ejecuta una vez al armar el modelo: aplica la configuración de tablas de cada contexto
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyInventoryConfiguration(); // tabla products
        builder.ApplySalesConfiguration();     // tabla orders
    }
}
