using Acme.Bakery.Platform.Inventory.Application.Acl;
using Acme.Bakery.Platform.Inventory.Application.CommandServices;
using Acme.Bakery.Platform.Inventory.Application.Internal.CommandServices;
using Acme.Bakery.Platform.Inventory.Application.Internal.QueryServices;
using Acme.Bakery.Platform.Inventory.Application.QueryServices;
using Acme.Bakery.Platform.Inventory.Domain.Repositories;
using Acme.Bakery.Platform.Inventory.Infrastructure.Persistence.EFC.Repositories;
using Acme.Bakery.Platform.Inventory.Interfaces.Acl;
using Acme.Bakery.Platform.Sales.Application.CommandServices;
using Acme.Bakery.Platform.Sales.Application.Internal.CommandServices;
using Acme.Bakery.Platform.Sales.Application.Internal.OutboundServices;
using Acme.Bakery.Platform.Sales.Application.Internal.QueryServices;
using Acme.Bakery.Platform.Sales.Application.QueryServices;
using Acme.Bakery.Platform.Sales.Domain.Repositories;
using Acme.Bakery.Platform.Sales.Infrastructure.Persistence.EFC.Repositories;
using Acme.Bakery.Platform.Shared.Domain.Repositories;
using Acme.Bakery.Platform.Shared.Infrastructure.Persistence.EFC.Configuration;
using Acme.Bakery.Platform.Shared.Infrastructure.Persistence.EFC.Repositories;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

// Punto de entrada de la aplicación: configura servicios, base de datos y el pipeline HTTP
var builder = WebApplication.CreateBuilder(args);

// ---------- Web API ----------
builder.Services.AddRouting(options => options.LowercaseUrls = true); // rutas en minúscula: /api/v1/products
builder.Services.AddControllers();                                    // habilita los controllers
builder.Services.AddProblemDetails();                                 // errores en formato ProblemDetails
builder.Services.AddOpenApi();                                        // genera la especificación OpenAPI

// ---------- Base de datos (MySQL + EF Core) ----------
// Lee la cadena de conexión de appsettings.json (o de una variable de entorno, que tiene prioridad)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                       ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// Registra AppDbContext usando MySQL y nombres en snake_case
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseMySQL(connectionString)
        .UseSnakeCaseNamingConvention() // CustomerName -> customer_name, CreatedAt -> created_at
        .EnableDetailedErrors();

    if (builder.Environment.IsDevelopment())
        options.EnableSensitiveDataLogging(); // muestra valores en los logs (solo en desarrollo)
});

// ---------- Inyección de dependencias ----------
// AddScoped: se crea una instancia nueva por cada petición HTTP
// Shared
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Bounded context Inventory
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IProductCommandService, ProductCommandService>();
builder.Services.AddScoped<IProductQueryService, ProductQueryService>();
builder.Services.AddScoped<IInventoryContextFacade, InventoryContextFacade>();

// Bounded context Sales
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IOrderCommandService, OrderCommandService>();
builder.Services.AddScoped<IOrderQueryService, OrderQueryService>();
builder.Services.AddScoped<ExternalInventoryService>();

var app = builder.Build();

// ---------- Crear la base de datos y las tablas si no existen ----------
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    context.Database.EnsureCreated();
}

// ---------- Pipeline HTTP ----------
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();              // /openapi/v1.json
    app.MapScalarApiReference();   // /scalar
}

app.UseHttpsRedirection(); // redirige de http a https si se usa el perfil https
app.MapControllers();      // conecta las rutas de los controllers

app.Run();                 // arranca el servidor
