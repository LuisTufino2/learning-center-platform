using Acme.Bakery.Platform.Sales.Domain.Model.Aggregates;
using Microsoft.EntityFrameworkCore;

namespace Acme.Bakery.Platform.Sales.Infrastructure.Persistence.EFC.Configuration.Extensions;

// Configuración de tablas del contexto Sales (Fluent API de EF Core).
public static class ModelBuilderExtensions
{
    // Define cómo se guarda Order en MySQL: tabla, clave, tamaños, decimales y estado como texto
    public static void ApplySalesConfiguration(this ModelBuilder builder)
    {
        builder.Entity<Order>().ToTable("orders");                                                // tabla en plural
        builder.Entity<Order>().HasKey(o => o.Id);                                                // clave primaria
        builder.Entity<Order>().Property(o => o.Id).IsRequired().ValueGeneratedOnAdd();           // autoincremental
        builder.Entity<Order>().Property(o => o.CustomerName).IsRequired().HasMaxLength(100);     // máximo 100 caracteres
        builder.Entity<Order>().Property(o => o.ProductId).IsRequired();                          // solo el id (sin FK a otro contexto)
        builder.Entity<Order>().Property(o => o.Quantity).IsRequired();
        builder.Entity<Order>().Property(o => o.UnitPrice).IsRequired().HasPrecision(10, 2);      // decimal(10,2)
        builder.Entity<Order>().Property(o => o.TotalAmount).IsRequired().HasPrecision(10, 2);    // decimal(10,2)
        builder.Entity<Order>().Property(o => o.PickupDate).IsRequired();
        builder.Entity<Order>().Property(o => o.Status).IsRequired()
            .HasConversion<string>().HasMaxLength(20); // se guarda como texto: "Pending", "Delivered"
    }
}
