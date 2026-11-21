using Acme.Bakery.Platform.Inventory.Domain.Model.Aggregates;
using Microsoft.EntityFrameworkCore;

namespace Acme.Bakery.Platform.Inventory.Infrastructure.Persistence.EFC.Configuration.Extensions;

// Configuración de tablas del contexto Inventory (Fluent API de EF Core).
public static class ModelBuilderExtensions
{
    // Define cómo se guarda Product en MySQL: tabla, clave, tamaños, índice único y tipos
    public static void ApplyInventoryConfiguration(this ModelBuilder builder)
    {
        builder.Entity<Product>().ToTable("products");                                      // tabla en plural
        builder.Entity<Product>().HasKey(p => p.Id);                                        // clave primaria
        builder.Entity<Product>().Property(p => p.Id).IsRequired().ValueGeneratedOnAdd();   // autoincremental
        builder.Entity<Product>().Property(p => p.Name).IsRequired().HasMaxLength(100);     // máximo 100 caracteres
        builder.Entity<Product>().HasIndex(p => p.Name).IsUnique();                         // nombre único
        builder.Entity<Product>().Property(p => p.Price).IsRequired().HasPrecision(10, 2);  // decimal(10,2)
        builder.Entity<Product>().Property(p => p.Stock).IsRequired();
        builder.Entity<Product>().Property(p => p.Category).IsRequired()
            .HasConversion<string>().HasMaxLength(20); // se guarda como texto: "Bread", "Cake", ...
    }
}
