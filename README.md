# Acme Bakery Platform: práctica para la PC2

API REST en **ASP.NET Core (.NET 10) + EF Core + MySQL** para una panadería, construida con la misma arquitectura que el ejemplo del profesor (`learning-center-platform`): **DDD con Bounded Contexts, CQRS, Repository + Unit of Work, Assemblers, Result pattern, ACL entre contextos y OpenAPI con Scalar**.

> Es una versión **simplificada** para practicar. Al final hay una sección con lo que tiene la versión completa del profesor y que aquí se omitió.

---

## Índice

1. [Enunciado](#1-enunciado)
2. [Solución paso a paso](#2-solución-paso-a-paso)
   - [Paso 0: Verificar herramientas](#paso-0-verificar-herramientas)
   - [Paso 1: Crear la solución y el proyecto](#paso-1-crear-la-solución-y-el-proyecto)
   - [Paso 2: Instalar paquetes NuGet](#paso-2-instalar-paquetes-nuget)
   - [Paso 3: Crear la estructura de carpetas](#paso-3-crear-la-estructura-de-carpetas)
   - [Paso 4: Configurar la conexión a MySQL](#paso-4-configurar-la-conexión-a-mysql)
   - [Paso 5: Shared (código común)](#paso-5-bounded-context-shared-código-común)
   - [Paso 6: Inventory (productos)](#paso-6-bounded-context-inventory-productos)
   - [Paso 7: Sales (pedidos)](#paso-7-bounded-context-sales-pedidos)
   - [Paso 8: Program.cs](#paso-8-programcs-inyección-de-dependencias-y-pipeline)
   - [Paso 9: Compilar y ejecutar](#paso-9-compilar-y-ejecutar)
   - [Paso 10: Probar la API](#paso-10-probar-la-api)
   - [Paso 11: Verificar en MySQL](#paso-11-verificar-en-mysql)
3. [Problemas comunes](#3-problemas-comunes)
4. [Conceptos que debes saber explicar](#4-conceptos-que-debes-saber-explicar)
5. [Diferencias con el proyecto del profesor](#5-diferencias-con-el-proyecto-del-profesor)
6. [Ejercicios extra para practicar](#6-ejercicios-extra-para-practicar)

---

## 1. Enunciado

> 📄 **El enunciado redactado en párrafos (estilo examen) y cómo separarlo paso a paso está en [interpretacion.md](interpretacion.md).** Abajo está el resumen ya separado en tablas.

**Acme Bakery** es una panadería que necesita un backend para registrar los productos que vende y los pedidos que sus clientes recogen en tienda.

Se te pide desarrollar una **API RESTful** con **ASP.NET Core (.NET 10)**, **Entity Framework Core** y **MySQL**, aplicando **Domain-Driven Design**. La solución debe llamarse `bakery-platform` y el proyecto `Acme.Bakery.Platform`.

### Bounded Context `Inventory`

Aggregate **`Product`**:

| Atributo    | Tipo       | Reglas                                                    |
|-------------|------------|-----------------------------------------------------------|
| `Id`        | `int`      | Autogenerado                                              |
| `Name`      | `string`   | Obligatorio, máx. 100 caracteres, **único**               |
| `Price`     | `decimal`  | Obligatorio, **mayor que 0**                              |
| `Stock`     | `int`      | Obligatorio, **no puede ser negativo**                    |
| `Category`  | enum       | `BREAD`, `CAKE`, `PASTRY`, `COOKIE` (se guarda como texto)|
| `CreatedAt` | `DateTime?`| Auditoría, lo asigna el sistema                           |
| `UpdatedAt` | `DateTime?`| Auditoría, lo asigna el sistema                           |

Endpoints:

| Método | Ruta                          | Respuestas                                   |
|--------|-------------------------------|----------------------------------------------|
| POST   | `/api/v1/products`            | `201` creado · `400` validación · `409` nombre duplicado |
| GET    | `/api/v1/products`            | `200`                                        |
| GET    | `/api/v1/products/{productId}`| `200` · `404`                                |

### Bounded Context `Sales`

Aggregate **`Order`**:

| Atributo       | Tipo        | Reglas                                                        |
|----------------|-------------|---------------------------------------------------------------|
| `Id`           | `int`       | Autogenerado                                                  |
| `CustomerName` | `string`    | Obligatorio, máx. 100 caracteres                              |
| `ProductId`    | `int`       | Debe existir en **Inventory**                                 |
| `Quantity`     | `int`       | **Mínimo 1** y **no mayor al stock disponible**               |
| `UnitPrice`    | `decimal`   | Lo obtiene el sistema del producto (no lo envía el cliente)   |
| `TotalAmount`  | `decimal`   | **Calculado**: `UnitPrice * Quantity`                         |
| `PickupDate`   | `DateTime`  | **No puede ser una fecha pasada**                             |
| `Status`       | enum        | `PENDING`, `DELIVERED`. Todo pedido nuevo inicia en `PENDING` |
| `CreatedAt`    | `DateTime?` | Auditoría                                                     |
| `UpdatedAt`    | `DateTime?` | Auditoría                                                     |

Endpoints:

| Método | Ruta                       | Respuestas                                                        |
|--------|----------------------------|-------------------------------------------------------------------|
| POST   | `/api/v1/orders`           | `201` · `400` validación · `404` producto no existe · `409` sin stock |
| GET    | `/api/v1/orders`           | `200`                                                             |
| GET    | `/api/v1/orders/{orderId}` | `200` · `404`                                                     |

### Reglas de negocio adicionales

1. Al registrar un pedido, **el stock del producto se descuenta** en la misma transacción en que se guarda el pedido.
2. `Sales` **no puede acceder directamente** a las clases de `Inventory`. Debe comunicarse mediante un **Anti-Corruption Layer (Facade)**.
3. Los errores se devuelven en formato **ProblemDetails (RFC 9457)**.

### Requisitos técnicos

- Base de datos **MySQL instalada localmente** (sin Docker), administrada con **MySQL Workbench**: usuario `root`, contraseña **`1234567`**, puerto `3306`, base de datos `acme_bakery_db`.
- La base de datos y las tablas se crean al iniciar la aplicación (`EnsureCreated`).
- Tablas en **plural** y columnas en **snake_case** (`products`, `orders`, `customer_name`, `created_at`…).
- Documentación de la API con **OpenAPI + Scalar** (`/scalar`).
- Estructura por capas en cada bounded context: `Domain`, `Application`, `Infrastructure`, `Interfaces`.

---

## 2. Solución paso a paso

> Todos los comandos son para **PowerShell** en Windows. Si usas Rider o Visual Studio puedes crear carpetas y clases con el IDE; los comandos son el equivalente.

### Paso 0: Verificar herramientas

> **No se usa Docker.** Se trabaja con el **MySQL Server instalado en la PC** y **MySQL Workbench**, como en los laboratorios de la universidad.

```powershell
dotnet --version          # Debe ser 10.x
dotnet --list-sdks
Get-Service *mysql*       # El servicio de MySQL debe estar "Running"
```

**Verificar la conexión en MySQL Workbench:**

1. Abre **MySQL Workbench**.
2. En *MySQL Connections* abre la conexión local (o crea una con **+**):
   - Hostname: `localhost` · Port: `3306` · Username: `root`
   - *Store in Vault…* → Password: `1234567`
3. Clic en **Test Connection** → debe decir *Successfully made the MySQL connection*.

> Si el servicio aparece detenido: `Start-Service <nombre-del-servicio>` en PowerShell como administrador, o desde *services.msc*.

### Paso 1: Crear la solución y el proyecto

```powershell
mkdir bakery-platform
cd bakery-platform

dotnet new sln -n bakery-platform --format sln       # igual que el profe (.sln, no .slnx)
dotnet new web -n Acme.Bakery.Platform -f net10.0    # proyecto ASP.NET Core vacío
dotnet sln bakery-platform.sln add Acme.Bakery.Platform/Acme.Bakery.Platform.csproj
dotnet new gitignore
```

### Paso 2: Instalar paquetes NuGet

> **Importante:** fija las versiones **10.x**. Si escribes `dotnet add package X` sin versión, NuGet puede bajar la 11.x, que **no es compatible con net10.0** (error `NU1202`).

```powershell
cd Acme.Bakery.Platform

dotnet add package Microsoft.EntityFrameworkCore --version 10.0.15
dotnet add package Microsoft.EntityFrameworkCore.Relational --version 10.0.15
dotnet add package MySql.EntityFrameworkCore --version 10.0.12          # provider oficial de Oracle: UseMySQL(...)
dotnet add package EFCore.NamingConventions --version 10.0.1             # UseSnakeCaseNamingConvention()
dotnet add package Microsoft.AspNetCore.OpenApi --version 10.0.15        # AddOpenApi / MapOpenApi
dotnet add package Scalar.AspNetCore --version 3.0.2                     # UI de documentación /scalar
```

El `.csproj` debe quedar así:

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="EFCore.NamingConventions" Version="10.0.1" />
    <PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="10.0.15" />
    <PackageReference Include="Microsoft.EntityFrameworkCore" Version="10.0.15" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Relational" Version="10.0.15" />
    <PackageReference Include="MySql.EntityFrameworkCore" Version="10.0.12" />
    <PackageReference Include="Scalar.AspNetCore" Version="3.0.2" />
  </ItemGroup>

</Project>
```

### Paso 3: Crear la estructura de carpetas

Cada bounded context tiene las 4 capas del profesor (`Domain`, `Application`, `Infrastructure`, `Interfaces`). Hay 3 formas de crearlas; usa la que prefieras.

#### Opción A: manual con el IDE (Rider o VS Code), sin comandos

Lo más práctico es **crear cada carpeta justo cuando vas a crear su primer archivo**, en este orden: Shared → Inventory → Sales, y dentro de cada uno Domain → Application → Infrastructure → Interfaces.

**Rider** (igual en Mac y Windows):
1. En el panel *Explorer* haz clic derecho sobre el proyecto `Acme.Bakery.Platform`.
2. **Add → New Directory** (en algunas versiones: *New → Directory*).
3. Escribe la ruta completa **con `/`**, por ejemplo `Shared/Domain/Model/Errors`, y presiona Enter: crea las 4 carpetas anidadas de una vez.
4. Clic derecho sobre la carpeta → **Add → Class/Interface** → escribe el nombre (por ejemplo `Error`). Rider pone el `namespace` correcto según la carpeta.

**VS Code**: en el explorador usa el ícono **New Folder** y escribe la ruta con `/` (`Shared/Domain/Model/Errors`). También puedes usar **New File** con `Shared/Domain/Model/Errors/Error.cs`, que crea las carpetas y el archivo a la vez.

Carpetas por crear (escríbelas tal cual en *New Directory*):

| Bounded context | Carpetas |
|---|---|
| **Shared** | `Shared/Domain/Model/Entities` · `Shared/Domain/Model/Errors` · `Shared/Domain/Repositories` · `Shared/Application/Model` · `Shared/Infrastructure/Persistence/EFC/Configuration` · `Shared/Infrastructure/Persistence/EFC/Interceptors` · `Shared/Infrastructure/Persistence/EFC/Repositories` · `Shared/Interfaces/Rest/Transform` |
| **Inventory** | `Inventory/Domain/Model/Aggregates` · `.../Commands` · `.../Queries` · `.../ValueObjects` · `.../Errors` · `Inventory/Domain/Repositories` · `Inventory/Application/CommandServices` · `Inventory/Application/QueryServices` · `Inventory/Application/Acl` · `Inventory/Application/Internal/CommandServices` · `Inventory/Application/Internal/QueryServices` · `Inventory/Infrastructure/Persistence/EFC/Configuration/Extensions` · `Inventory/Infrastructure/Persistence/EFC/Repositories` · `Inventory/Interfaces/Acl` · `Inventory/Interfaces/Rest/Resources` · `Inventory/Interfaces/Rest/Transform` |
| **Sales** | Igual que Inventory, pero **sin** `Application/Acl` ni `Interfaces/Acl`, y **con** `Sales/Application/Internal/OutboundServices` |

#### Opción B: terminal de Mac (o Linux / Git Bash)

`mkdir -p` crea las carpetas intermedias y `{A,B}` crea varias hermanas a la vez:

```bash
# Ejecutar dentro de Acme.Bakery.Platform/
mkdir -p Shared/Domain/Model/{Entities,Errors} Shared/Domain/Repositories Shared/Application/Model \
  Shared/Infrastructure/Persistence/EFC/{Configuration,Interceptors,Repositories} Shared/Interfaces/Rest/Transform

mkdir -p Inventory/Domain/Model/{Aggregates,Commands,Queries,ValueObjects,Errors} Inventory/Domain/Repositories \
  Inventory/Application/{CommandServices,QueryServices,Acl} Inventory/Application/Internal/{CommandServices,QueryServices} \
  Inventory/Infrastructure/Persistence/EFC/Configuration/Extensions Inventory/Infrastructure/Persistence/EFC/Repositories \
  Inventory/Interfaces/Acl Inventory/Interfaces/Rest/{Resources,Transform}

mkdir -p Sales/Domain/Model/{Aggregates,Commands,Queries,ValueObjects,Errors} Sales/Domain/Repositories \
  Sales/Application/{CommandServices,QueryServices} Sales/Application/Internal/{CommandServices,QueryServices,OutboundServices} \
  Sales/Infrastructure/Persistence/EFC/Configuration/Extensions Sales/Infrastructure/Persistence/EFC/Repositories \
  Sales/Interfaces/Rest/{Resources,Transform}
```

> Ojo: **sin espacios** dentro de las llaves (`{Entities,Errors}`, no `{Entities, Errors}`).

#### Opción C: PowerShell en Windows

```powershell
# Ejecutar dentro de Acme.Bakery.Platform/
$folders = @(
  "Shared/Domain/Model/Entities", "Shared/Domain/Model/Errors", "Shared/Domain/Repositories",
  "Shared/Application/Model",
  "Shared/Infrastructure/Persistence/EFC/Configuration",
  "Shared/Infrastructure/Persistence/EFC/Interceptors",
  "Shared/Infrastructure/Persistence/EFC/Repositories",
  "Shared/Interfaces/Rest/Transform",

  "Inventory/Domain/Model/Aggregates", "Inventory/Domain/Model/Commands", "Inventory/Domain/Model/Queries",
  "Inventory/Domain/Model/ValueObjects", "Inventory/Domain/Model/Errors", "Inventory/Domain/Repositories",
  "Inventory/Application/CommandServices", "Inventory/Application/QueryServices", "Inventory/Application/Acl",
  "Inventory/Application/Internal/CommandServices", "Inventory/Application/Internal/QueryServices",
  "Inventory/Infrastructure/Persistence/EFC/Configuration/Extensions",
  "Inventory/Infrastructure/Persistence/EFC/Repositories",
  "Inventory/Interfaces/Acl", "Inventory/Interfaces/Rest/Resources", "Inventory/Interfaces/Rest/Transform",

  "Sales/Domain/Model/Aggregates", "Sales/Domain/Model/Commands", "Sales/Domain/Model/Queries",
  "Sales/Domain/Model/ValueObjects", "Sales/Domain/Model/Errors", "Sales/Domain/Repositories",
  "Sales/Application/CommandServices", "Sales/Application/QueryServices",
  "Sales/Application/Internal/CommandServices", "Sales/Application/Internal/QueryServices",
  "Sales/Application/Internal/OutboundServices",
  "Sales/Infrastructure/Persistence/EFC/Configuration/Extensions",
  "Sales/Infrastructure/Persistence/EFC/Repositories",
  "Sales/Interfaces/Rest/Resources", "Sales/Interfaces/Rest/Transform"
)
$folders | ForEach-Object { New-Item -ItemType Directory -Force -Path $_ | Out-Null }
```

Resultado final:

```
Acme.Bakery.Platform/
├── Program.cs
├── appsettings.json
├── Acme.Bakery.Platform.http          ← peticiones de prueba
├── Shared/                            ← código común (kernel compartido)
│   ├── Domain/        (Error, IAuditableEntity, IBaseRepository, IUnitOfWork)
│   ├── Application/   (Result<T>)
│   ├── Infrastructure/(AppDbContext, AuditableEntityInterceptor, BaseRepository, UnitOfWork)
│   └── Interfaces/    (ErrorActionResultAssembler)
├── Inventory/                         ← BC de productos
│   ├── Domain/        (Product, comandos, queries, errores, IProductRepository)
│   ├── Application/   (servicios de comandos/queries, Facade ACL)
│   ├── Infrastructure/(mapeo EF Core, ProductRepository)
│   └── Interfaces/    (ProductsController, Resources, Assemblers, IInventoryContextFacade)
└── Sales/                             ← BC de pedidos
    ├── Domain/        (Order, comandos, queries, errores, IOrderRepository)
    ├── Application/   (servicios, ExternalInventoryService ← usa el ACL)
    ├── Infrastructure/(mapeo EF Core, OrderRepository)
    └── Interfaces/    (OrdersController, Resources, Assemblers)
```

### Paso 4: Configurar la conexión a MySQL

`appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "server=localhost;port=3306;user=root;password=1234567;database=acme_bakery_db;"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

> No necesitas crear la base de datos en Workbench: `EnsureCreated()` la crea al ejecutar la app.

En `Properties/launchSettings.json` agrega `"launchUrl": "scalar"` a los perfiles `http` y `https` para que se abra la documentación al ejecutar.

---

### Paso 5: Bounded Context `Shared` (código común)

**`Shared/Domain/Model/Errors/Error.cs`**: tipo de error y record de error.

```csharp
namespace Acme.Bakery.Platform.Shared.Domain.Model.Errors;

public enum ErrorType
{
    Validation, // 400 Bad Request
    NotFound,   // 404 Not Found
    Conflict    // 409 Conflict
}

public record Error(string Code, string Message, ErrorType Type)
{
    public static Error Validation(string code, string message) => new(code, message, ErrorType.Validation);
    public static Error NotFound(string code, string message) => new(code, message, ErrorType.NotFound);
    public static Error Conflict(string code, string message) => new(code, message, ErrorType.Conflict);
}
```

**`Shared/Application/Model/Result.cs`**: el **Result pattern**: un comando devuelve éxito o error, sin lanzar excepciones.

```csharp
using Acme.Bakery.Platform.Shared.Domain.Model.Errors;

namespace Acme.Bakery.Platform.Shared.Application.Model;

public sealed class Result<T>
{
    private Result(T? value, Error? error)
    {
        Value = value;
        Error = error;
    }

    public T? Value { get; }
    public Error? Error { get; }
    public bool IsSuccess => Error is null;
    public bool IsFailure => !IsSuccess;

    public static Result<T> Success(T value) => new(value, null);
    public static Result<T> Failure(Error error) => new(default, error);
}
```

**`Shared/Domain/Model/Entities/IAuditableEntity.cs`**

```csharp
namespace Acme.Bakery.Platform.Shared.Domain.Model.Entities;

public interface IAuditableEntity
{
    DateTime? CreatedAt { get; set; }
    DateTime? UpdatedAt { get; set; }
}
```

**`Shared/Domain/Repositories/IBaseRepository.cs`** y **`IUnitOfWork.cs`**: contratos (el Domain define, Infrastructure implementa).

```csharp
namespace Acme.Bakery.Platform.Shared.Domain.Repositories;

public interface IBaseRepository<TEntity>
{
    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task<TEntity?> FindByIdAsync(int id, CancellationToken cancellationToken = default);
    void Update(TEntity entity);
    void Remove(TEntity entity);
    Task<IEnumerable<TEntity>> ListAsync(CancellationToken cancellationToken = default);
}
```

```csharp
namespace Acme.Bakery.Platform.Shared.Domain.Repositories;

public interface IUnitOfWork
{
    Task CompleteAsync(CancellationToken cancellationToken = default);
}
```

**`Shared/Infrastructure/Persistence/EFC/Configuration/AppDbContext.cs`**: un solo DbContext; cada BC aporta su configuración con un *extension method*.

```csharp
using Acme.Bakery.Platform.Inventory.Infrastructure.Persistence.EFC.Configuration.Extensions;
using Acme.Bakery.Platform.Sales.Infrastructure.Persistence.EFC.Configuration.Extensions;
using Acme.Bakery.Platform.Shared.Infrastructure.Persistence.EFC.Interceptors;
using Microsoft.EntityFrameworkCore;

namespace Acme.Bakery.Platform.Shared.Infrastructure.Persistence.EFC.Configuration;

public class AppDbContext(DbContextOptions options) : DbContext(options)
{
    protected override void OnConfiguring(DbContextOptionsBuilder builder)
    {
        builder.AddInterceptors(new AuditableEntityInterceptor());
        base.OnConfiguring(builder);
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyInventoryConfiguration();
        builder.ApplySalesConfiguration();
    }
}
```

**`Shared/Infrastructure/Persistence/EFC/Interceptors/AuditableEntityInterceptor.cs`**: llena `created_at` / `updated_at` justo antes de guardar.

```csharp
using Acme.Bakery.Platform.Shared.Domain.Model.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Acme.Bakery.Platform.Shared.Infrastructure.Persistence.EFC.Interceptors;

public class AuditableEntityInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        UpdateAuditFields(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
        InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        UpdateAuditFields(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void UpdateAuditFields(DbContext? context)
    {
        if (context is null) return;
        var now = DateTime.UtcNow;

        foreach (var entry in context.ChangeTracker.Entries<IAuditableEntity>())
        {
            if (entry.State == EntityState.Added)
                entry.Entity.CreatedAt = now;

            if (entry.State is EntityState.Added or EntityState.Modified)
                entry.Entity.UpdatedAt = now;
        }
    }
}
```

**`Shared/Infrastructure/Persistence/EFC/Repositories/BaseRepository.cs`** y **`UnitOfWork.cs`**

```csharp
using Acme.Bakery.Platform.Shared.Domain.Repositories;
using Acme.Bakery.Platform.Shared.Infrastructure.Persistence.EFC.Configuration;
using Microsoft.EntityFrameworkCore;

namespace Acme.Bakery.Platform.Shared.Infrastructure.Persistence.EFC.Repositories;

public abstract class BaseRepository<TEntity>(AppDbContext context) : IBaseRepository<TEntity>
    where TEntity : class
{
    protected readonly AppDbContext Context = context;

    public async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        await Context.Set<TEntity>().AddAsync(entity, cancellationToken);
    }

    public async Task<TEntity?> FindByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await Context.Set<TEntity>().FindAsync([id], cancellationToken);
    }

    public void Update(TEntity entity) => Context.Set<TEntity>().Update(entity);

    public void Remove(TEntity entity) => Context.Set<TEntity>().Remove(entity);

    public async Task<IEnumerable<TEntity>> ListAsync(CancellationToken cancellationToken = default)
    {
        return await Context.Set<TEntity>().ToListAsync(cancellationToken);
    }
}
```

```csharp
using Acme.Bakery.Platform.Shared.Domain.Repositories;
using Acme.Bakery.Platform.Shared.Infrastructure.Persistence.EFC.Configuration;

namespace Acme.Bakery.Platform.Shared.Infrastructure.Persistence.EFC.Repositories;

public class UnitOfWork(AppDbContext context) : IUnitOfWork
{
    public async Task CompleteAsync(CancellationToken cancellationToken = default)
    {
        await context.SaveChangesAsync(cancellationToken);
    }
}
```

**`Shared/Interfaces/Rest/Transform/ErrorActionResultAssembler.cs`**: convierte `Error` en respuesta HTTP con ProblemDetails.

```csharp
using Acme.Bakery.Platform.Shared.Domain.Model.Errors;
using Microsoft.AspNetCore.Mvc;

namespace Acme.Bakery.Platform.Shared.Interfaces.Rest.Transform;

public static class ErrorActionResultAssembler
{
    public static IActionResult ToActionResultFromError(ControllerBase controller, Error error)
    {
        var statusCode = error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError
        };

        return controller.Problem(title: error.Code, detail: error.Message, statusCode: statusCode);
    }
}
```

---

### Paso 6: Bounded Context `Inventory` (productos)

#### 6.1 Domain

**`Inventory/Domain/Model/ValueObjects/EProductCategory.cs`**

```csharp
namespace Acme.Bakery.Platform.Inventory.Domain.Model.ValueObjects;

public enum EProductCategory
{
    Bread,
    Cake,
    Pastry,
    Cookie
}
```

**`Inventory/Domain/Model/Aggregates/Product.cs`**: Aggregate Root. Setters privados: el estado solo cambia con sus métodos.

```csharp
using Acme.Bakery.Platform.Inventory.Domain.Model.ValueObjects;
using Acme.Bakery.Platform.Shared.Domain.Model.Entities;

namespace Acme.Bakery.Platform.Inventory.Domain.Model.Aggregates;

public class Product : IAuditableEntity
{
    // EF Core necesita un constructor sin parámetros
    private Product()
    {
        Name = string.Empty;
    }

    public Product(string name, decimal price, int stock, EProductCategory category)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(price);
        ArgumentOutOfRangeException.ThrowIfNegative(stock);

        Name = name.Trim();
        Price = price;
        Stock = stock;
        Category = category;
    }

    public int Id { get; private set; }
    public string Name { get; private set; }
    public decimal Price { get; private set; }
    public int Stock { get; private set; }
    public EProductCategory Category { get; private set; }

    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public bool HasStockFor(int quantity) => quantity > 0 && Stock >= quantity;

    public void DecreaseStock(int quantity)
    {
        if (!HasStockFor(quantity))
            throw new InvalidOperationException($"Not enough stock for product {Id}.");
        Stock -= quantity;
    }
}
```

**Commands y Queries** (records inmutables, un archivo por record):

```csharp
// Inventory/Domain/Model/Commands/CreateProductCommand.cs
namespace Acme.Bakery.Platform.Inventory.Domain.Model.Commands;
public record CreateProductCommand(string Name, decimal Price, int Stock, string Category);

// Inventory/Domain/Model/Commands/DecreaseProductStockCommand.cs
namespace Acme.Bakery.Platform.Inventory.Domain.Model.Commands;
public record DecreaseProductStockCommand(int ProductId, int Quantity);

// Inventory/Domain/Model/Queries/GetProductByIdQuery.cs
namespace Acme.Bakery.Platform.Inventory.Domain.Model.Queries;
public record GetProductByIdQuery(int ProductId);

// Inventory/Domain/Model/Queries/GetAllProductsQuery.cs
namespace Acme.Bakery.Platform.Inventory.Domain.Model.Queries;
public record GetAllProductsQuery;
```

**`Inventory/Domain/Model/Errors/InventoryErrors.cs`**: catálogo de errores del BC.

```csharp
using Acme.Bakery.Platform.Shared.Domain.Model.Errors;

namespace Acme.Bakery.Platform.Inventory.Domain.Model.Errors;

public static class InventoryErrors
{
    public static readonly Error NameRequired =
        Error.Validation("Inventory.NameRequired", "Product name is required.");

    public static readonly Error InvalidPrice =
        Error.Validation("Inventory.InvalidPrice", "Price must be greater than 0.");

    public static readonly Error InvalidStock =
        Error.Validation("Inventory.InvalidStock", "Stock cannot be negative.");

    public static readonly Error InvalidCategory =
        Error.Validation("Inventory.InvalidCategory", "Category must be one of: BREAD, CAKE, PASTRY, COOKIE.");

    public static readonly Error NameAlreadyExists =
        Error.Conflict("Inventory.NameAlreadyExists", "A product with the same name already exists.");

    public static readonly Error ProductNotFound =
        Error.NotFound("Inventory.ProductNotFound", "The specified product was not found.");

    public static readonly Error InsufficientStock =
        Error.Conflict("Inventory.InsufficientStock", "There is not enough stock for the requested quantity.");
}
```

**`Inventory/Domain/Repositories/IProductRepository.cs`**

```csharp
using Acme.Bakery.Platform.Inventory.Domain.Model.Aggregates;
using Acme.Bakery.Platform.Shared.Domain.Repositories;

namespace Acme.Bakery.Platform.Inventory.Domain.Repositories;

public interface IProductRepository : IBaseRepository<Product>
{
    Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken = default);
}
```

#### 6.2 Application (CQRS: comandos y queries separados)

**Interfaces** (`Inventory/Application/CommandServices/IProductCommandService.cs` y `Inventory/Application/QueryServices/IProductQueryService.cs`):

```csharp
using Acme.Bakery.Platform.Inventory.Domain.Model.Aggregates;
using Acme.Bakery.Platform.Inventory.Domain.Model.Commands;
using Acme.Bakery.Platform.Shared.Application.Model;

namespace Acme.Bakery.Platform.Inventory.Application.CommandServices;

public interface IProductCommandService
{
    Task<Result<Product>> Handle(CreateProductCommand command, CancellationToken cancellationToken);
    Task<Result<Product>> Handle(DecreaseProductStockCommand command, CancellationToken cancellationToken);
}
```

```csharp
using Acme.Bakery.Platform.Inventory.Domain.Model.Aggregates;
using Acme.Bakery.Platform.Inventory.Domain.Model.Queries;

namespace Acme.Bakery.Platform.Inventory.Application.QueryServices;

public interface IProductQueryService
{
    Task<Product?> Handle(GetProductByIdQuery query, CancellationToken cancellationToken);
    Task<IEnumerable<Product>> Handle(GetAllProductsQuery query, CancellationToken cancellationToken);
}
```

**`Inventory/Application/Internal/CommandServices/ProductCommandService.cs`**: aquí van las **reglas de negocio**.

```csharp
using Acme.Bakery.Platform.Inventory.Application.CommandServices;
using Acme.Bakery.Platform.Inventory.Domain.Model.Aggregates;
using Acme.Bakery.Platform.Inventory.Domain.Model.Commands;
using Acme.Bakery.Platform.Inventory.Domain.Model.Errors;
using Acme.Bakery.Platform.Inventory.Domain.Model.ValueObjects;
using Acme.Bakery.Platform.Inventory.Domain.Repositories;
using Acme.Bakery.Platform.Shared.Application.Model;
using Acme.Bakery.Platform.Shared.Domain.Repositories;

namespace Acme.Bakery.Platform.Inventory.Application.Internal.CommandServices;

public class ProductCommandService(IProductRepository productRepository, IUnitOfWork unitOfWork)
    : IProductCommandService
{
    public async Task<Result<Product>> Handle(CreateProductCommand command, CancellationToken cancellationToken)
    {
        // 1. Validar reglas de negocio
        if (string.IsNullOrWhiteSpace(command.Name))
            return Result<Product>.Failure(InventoryErrors.NameRequired);

        if (command.Price <= 0)
            return Result<Product>.Failure(InventoryErrors.InvalidPrice);

        if (command.Stock < 0)
            return Result<Product>.Failure(InventoryErrors.InvalidStock);

        if (!Enum.TryParse<EProductCategory>(command.Category, true, out var category)
            || !Enum.IsDefined(category)
            || int.TryParse(command.Category, out _))
            return Result<Product>.Failure(InventoryErrors.InvalidCategory);

        if (await productRepository.ExistsByNameAsync(command.Name.Trim(), cancellationToken))
            return Result<Product>.Failure(InventoryErrors.NameAlreadyExists);

        // 2. Crear el aggregate y persistir
        var product = new Product(command.Name, command.Price, command.Stock, category);
        await productRepository.AddAsync(product, cancellationToken);
        await unitOfWork.CompleteAsync(cancellationToken);

        return Result<Product>.Success(product);
    }

    // NO llama a CompleteAsync(): lo usa Sales (vía ACL) y Sales guarda el stock
    // y el pedido nuevo en un solo SaveChanges (una sola transacción).
    public async Task<Result<Product>> Handle(DecreaseProductStockCommand command,
        CancellationToken cancellationToken)
    {
        var product = await productRepository.FindByIdAsync(command.ProductId, cancellationToken);
        if (product is null)
            return Result<Product>.Failure(InventoryErrors.ProductNotFound);

        if (!product.HasStockFor(command.Quantity))
            return Result<Product>.Failure(InventoryErrors.InsufficientStock);

        product.DecreaseStock(command.Quantity);
        productRepository.Update(product);

        return Result<Product>.Success(product);
    }
}
```

**`Inventory/Application/Internal/QueryServices/ProductQueryService.cs`**

```csharp
using Acme.Bakery.Platform.Inventory.Application.QueryServices;
using Acme.Bakery.Platform.Inventory.Domain.Model.Aggregates;
using Acme.Bakery.Platform.Inventory.Domain.Model.Queries;
using Acme.Bakery.Platform.Inventory.Domain.Repositories;

namespace Acme.Bakery.Platform.Inventory.Application.Internal.QueryServices;

public class ProductQueryService(IProductRepository productRepository) : IProductQueryService
{
    public async Task<Product?> Handle(GetProductByIdQuery query, CancellationToken cancellationToken)
    {
        return await productRepository.FindByIdAsync(query.ProductId, cancellationToken);
    }

    public async Task<IEnumerable<Product>> Handle(GetAllProductsQuery query, CancellationToken cancellationToken)
    {
        return await productRepository.ListAsync(cancellationToken);
    }
}
```

#### 6.3 ACL: lo que Inventory **expone** a otros contextos

**`Inventory/Interfaces/Acl/IInventoryContextFacade.cs`**: solo entran y salen **primitivos** (nunca el aggregate `Product`).

```csharp
namespace Acme.Bakery.Platform.Inventory.Interfaces.Acl;

public interface IInventoryContextFacade
{
    /// <returns>El precio, o null si el producto no existe.</returns>
    Task<decimal?> FetchProductPriceByIdAsync(int productId, CancellationToken cancellationToken);

    /// <returns>true si se descontó el stock; false si no existe o no alcanza.</returns>
    Task<bool> DecreaseProductStockAsync(int productId, int quantity, CancellationToken cancellationToken);
}
```

**`Inventory/Application/Acl/InventoryContextFacade.cs`**: implementación que reutiliza los servicios del propio BC.

```csharp
using Acme.Bakery.Platform.Inventory.Application.CommandServices;
using Acme.Bakery.Platform.Inventory.Application.QueryServices;
using Acme.Bakery.Platform.Inventory.Domain.Model.Commands;
using Acme.Bakery.Platform.Inventory.Domain.Model.Queries;
using Acme.Bakery.Platform.Inventory.Interfaces.Acl;

namespace Acme.Bakery.Platform.Inventory.Application.Acl;

public class InventoryContextFacade(
    IProductCommandService productCommandService,
    IProductQueryService productQueryService) : IInventoryContextFacade
{
    public async Task<decimal?> FetchProductPriceByIdAsync(int productId, CancellationToken cancellationToken)
    {
        var product = await productQueryService.Handle(new GetProductByIdQuery(productId), cancellationToken);
        return product?.Price;
    }

    public async Task<bool> DecreaseProductStockAsync(int productId, int quantity,
        CancellationToken cancellationToken)
    {
        var result = await productCommandService.Handle(
            new DecreaseProductStockCommand(productId, quantity), cancellationToken);
        return result.IsSuccess;
    }
}
```

#### 6.4 Infrastructure (EF Core)

**`Inventory/Infrastructure/Persistence/EFC/Configuration/Extensions/ModelBuilderExtensions.cs`**: Fluent API.

```csharp
using Acme.Bakery.Platform.Inventory.Domain.Model.Aggregates;
using Microsoft.EntityFrameworkCore;

namespace Acme.Bakery.Platform.Inventory.Infrastructure.Persistence.EFC.Configuration.Extensions;

public static class ModelBuilderExtensions
{
    public static void ApplyInventoryConfiguration(this ModelBuilder builder)
    {
        builder.Entity<Product>().ToTable("products");
        builder.Entity<Product>().HasKey(p => p.Id);
        builder.Entity<Product>().Property(p => p.Id).IsRequired().ValueGeneratedOnAdd();
        builder.Entity<Product>().Property(p => p.Name).IsRequired().HasMaxLength(100);
        builder.Entity<Product>().HasIndex(p => p.Name).IsUnique();
        builder.Entity<Product>().Property(p => p.Price).IsRequired().HasPrecision(10, 2);
        builder.Entity<Product>().Property(p => p.Stock).IsRequired();
        builder.Entity<Product>().Property(p => p.Category).IsRequired()
            .HasConversion<string>().HasMaxLength(20); // se guarda como texto: "Bread", "Cake", ...
    }
}
```

**`Inventory/Infrastructure/Persistence/EFC/Repositories/ProductRepository.cs`**

```csharp
using Acme.Bakery.Platform.Inventory.Domain.Model.Aggregates;
using Acme.Bakery.Platform.Inventory.Domain.Repositories;
using Acme.Bakery.Platform.Shared.Infrastructure.Persistence.EFC.Configuration;
using Acme.Bakery.Platform.Shared.Infrastructure.Persistence.EFC.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Acme.Bakery.Platform.Inventory.Infrastructure.Persistence.EFC.Repositories;

public class ProductRepository(AppDbContext context) : BaseRepository<Product>(context), IProductRepository
{
    public async Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return await Context.Set<Product>().AnyAsync(p => p.Name == name, cancellationToken);
    }
}
```

#### 6.5 Interfaces (REST)

**Resources** (DTOs que viajan por HTTP):

```csharp
// Inventory/Interfaces/Rest/Resources/CreateProductResource.cs
namespace Acme.Bakery.Platform.Inventory.Interfaces.Rest.Resources;
public record CreateProductResource(string Name, decimal Price, int Stock, string Category);

// Inventory/Interfaces/Rest/Resources/ProductResource.cs
namespace Acme.Bakery.Platform.Inventory.Interfaces.Rest.Resources;
public record ProductResource(int Id, string Name, decimal Price, int Stock, string Category,
    DateTime? CreatedAt, DateTime? UpdatedAt);
```

**Assemblers** (Resource ⇄ Command / Entity ⇄ Resource):

```csharp
// Inventory/Interfaces/Rest/Transform/CreateProductCommandFromResourceAssembler.cs
using Acme.Bakery.Platform.Inventory.Domain.Model.Commands;
using Acme.Bakery.Platform.Inventory.Interfaces.Rest.Resources;

namespace Acme.Bakery.Platform.Inventory.Interfaces.Rest.Transform;

public static class CreateProductCommandFromResourceAssembler
{
    public static CreateProductCommand ToCommandFromResource(CreateProductResource resource)
    {
        return new CreateProductCommand(resource.Name, resource.Price, resource.Stock, resource.Category);
    }
}
```

```csharp
// Inventory/Interfaces/Rest/Transform/ProductResourceFromEntityAssembler.cs
using Acme.Bakery.Platform.Inventory.Domain.Model.Aggregates;
using Acme.Bakery.Platform.Inventory.Interfaces.Rest.Resources;

namespace Acme.Bakery.Platform.Inventory.Interfaces.Rest.Transform;

public static class ProductResourceFromEntityAssembler
{
    public static ProductResource ToResourceFromEntity(Product entity)
    {
        return new ProductResource(
            entity.Id,
            entity.Name,
            entity.Price,
            entity.Stock,
            entity.Category.ToString().ToUpperInvariant(),
            entity.CreatedAt,
            entity.UpdatedAt);
    }
}
```

**`Inventory/Interfaces/Rest/ProductsController.cs`**: controlador delgado: Resource → Command → Service → Result → HTTP.

```csharp
using System.Net.Mime;
using Acme.Bakery.Platform.Inventory.Application.CommandServices;
using Acme.Bakery.Platform.Inventory.Application.QueryServices;
using Acme.Bakery.Platform.Inventory.Domain.Model.Queries;
using Acme.Bakery.Platform.Inventory.Interfaces.Rest.Resources;
using Acme.Bakery.Platform.Inventory.Interfaces.Rest.Transform;
using Acme.Bakery.Platform.Shared.Interfaces.Rest.Transform;
using Microsoft.AspNetCore.Mvc;

namespace Acme.Bakery.Platform.Inventory.Interfaces.Rest;

[ApiController]
[Route("api/v1/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
[Tags("Products")]
public class ProductsController(
    IProductCommandService productCommandService,
    IProductQueryService productQueryService) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(ProductResource), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateProduct([FromBody] CreateProductResource resource,
        CancellationToken cancellationToken)
    {
        var command = CreateProductCommandFromResourceAssembler.ToCommandFromResource(resource);
        var result = await productCommandService.Handle(command, cancellationToken);

        if (result.IsFailure)
            return ErrorActionResultAssembler.ToActionResultFromError(this, result.Error!);

        var productResource = ProductResourceFromEntityAssembler.ToResourceFromEntity(result.Value!);
        return CreatedAtAction(nameof(GetProductById), new { productId = productResource.Id }, productResource);
    }

    [HttpGet("{productId:int}")]
    [ProducesResponseType(typeof(ProductResource), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProductById(int productId, CancellationToken cancellationToken)
    {
        var product = await productQueryService.Handle(new GetProductByIdQuery(productId), cancellationToken);
        if (product is null) return NotFound();
        return Ok(ProductResourceFromEntityAssembler.ToResourceFromEntity(product));
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ProductResource>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllProducts(CancellationToken cancellationToken)
    {
        var products = await productQueryService.Handle(new GetAllProductsQuery(), cancellationToken);
        return Ok(products.Select(ProductResourceFromEntityAssembler.ToResourceFromEntity));
    }
}
```

---

### Paso 7: Bounded Context `Sales` (pedidos)

#### 7.1 Domain

```csharp
// Sales/Domain/Model/ValueObjects/EOrderStatus.cs
namespace Acme.Bakery.Platform.Sales.Domain.Model.ValueObjects;

public enum EOrderStatus
{
    Pending,
    Delivered
}
```

**`Sales/Domain/Model/Aggregates/Order.cs`**: guarda solo el `ProductId` (no un objeto `Product`) porque `Product` pertenece a otro BC.

```csharp
using Acme.Bakery.Platform.Sales.Domain.Model.ValueObjects;
using Acme.Bakery.Platform.Shared.Domain.Model.Entities;

namespace Acme.Bakery.Platform.Sales.Domain.Model.Aggregates;

public class Order : IAuditableEntity
{
    private Order()
    {
        CustomerName = string.Empty;
    }

    public Order(string customerName, int productId, int quantity, decimal unitPrice, DateTime pickupDate)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(customerName);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);

        CustomerName = customerName.Trim();
        ProductId = productId;
        Quantity = quantity;
        UnitPrice = unitPrice;
        TotalAmount = unitPrice * quantity; // regla: el total se calcula, no lo envía el cliente
        PickupDate = pickupDate;
        Status = EOrderStatus.Pending;      // regla: todo pedido nuevo inicia en PENDING
    }

    public int Id { get; private set; }
    public string CustomerName { get; private set; }
    public int ProductId { get; private set; }
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal TotalAmount { get; private set; }
    public DateTime PickupDate { get; private set; }
    public EOrderStatus Status { get; private set; }

    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
```

```csharp
// Sales/Domain/Model/Commands/CreateOrderCommand.cs
namespace Acme.Bakery.Platform.Sales.Domain.Model.Commands;
public record CreateOrderCommand(string CustomerName, int ProductId, int Quantity, DateTime PickupDate);

// Sales/Domain/Model/Queries/GetOrderByIdQuery.cs
namespace Acme.Bakery.Platform.Sales.Domain.Model.Queries;
public record GetOrderByIdQuery(int OrderId);

// Sales/Domain/Model/Queries/GetAllOrdersQuery.cs
namespace Acme.Bakery.Platform.Sales.Domain.Model.Queries;
public record GetAllOrdersQuery;

// Sales/Domain/Repositories/IOrderRepository.cs
using Acme.Bakery.Platform.Sales.Domain.Model.Aggregates;
using Acme.Bakery.Platform.Shared.Domain.Repositories;
namespace Acme.Bakery.Platform.Sales.Domain.Repositories;
public interface IOrderRepository : IBaseRepository<Order>;
```

**`Sales/Domain/Model/Errors/SalesErrors.cs`**

```csharp
using Acme.Bakery.Platform.Shared.Domain.Model.Errors;

namespace Acme.Bakery.Platform.Sales.Domain.Model.Errors;

public static class SalesErrors
{
    public static readonly Error CustomerNameRequired =
        Error.Validation("Sales.CustomerNameRequired", "Customer name is required.");

    public static readonly Error InvalidQuantity =
        Error.Validation("Sales.InvalidQuantity", "Quantity must be at least 1.");

    public static readonly Error PickupDateInPast =
        Error.Validation("Sales.PickupDateInPast", "Pickup date cannot be in the past.");

    public static readonly Error ProductNotFound =
        Error.NotFound("Sales.ProductNotFound", "The requested product does not exist.");

    public static readonly Error InsufficientStock =
        Error.Conflict("Sales.InsufficientStock", "There is not enough stock for the requested quantity.");
}
```

#### 7.2 Application

**Interfaces** (`Sales/Application/CommandServices/IOrderCommandService.cs`, `Sales/Application/QueryServices/IOrderQueryService.cs`):

```csharp
using Acme.Bakery.Platform.Sales.Domain.Model.Aggregates;
using Acme.Bakery.Platform.Sales.Domain.Model.Commands;
using Acme.Bakery.Platform.Shared.Application.Model;

namespace Acme.Bakery.Platform.Sales.Application.CommandServices;

public interface IOrderCommandService
{
    Task<Result<Order>> Handle(CreateOrderCommand command, CancellationToken cancellationToken);
}
```

```csharp
using Acme.Bakery.Platform.Sales.Domain.Model.Aggregates;
using Acme.Bakery.Platform.Sales.Domain.Model.Queries;

namespace Acme.Bakery.Platform.Sales.Application.QueryServices;

public interface IOrderQueryService
{
    Task<Order?> Handle(GetOrderByIdQuery query, CancellationToken cancellationToken);
    Task<IEnumerable<Order>> Handle(GetAllOrdersQuery query, CancellationToken cancellationToken);
}
```

**`Sales/Application/Internal/OutboundServices/ExternalInventoryService.cs`**: el **único** lugar de Sales que conoce a Inventory.

```csharp
using Acme.Bakery.Platform.Inventory.Interfaces.Acl;

namespace Acme.Bakery.Platform.Sales.Application.Internal.OutboundServices;

public class ExternalInventoryService(IInventoryContextFacade inventoryContextFacade)
{
    public Task<decimal?> FetchProductPriceAsync(int productId, CancellationToken cancellationToken)
    {
        return inventoryContextFacade.FetchProductPriceByIdAsync(productId, cancellationToken);
    }

    public Task<bool> ReserveStockAsync(int productId, int quantity, CancellationToken cancellationToken)
    {
        return inventoryContextFacade.DecreaseProductStockAsync(productId, quantity, cancellationToken);
    }
}
```

**`Sales/Application/Internal/CommandServices/OrderCommandService.cs`**: el corazón del ejercicio.

```csharp
using Acme.Bakery.Platform.Sales.Application.CommandServices;
using Acme.Bakery.Platform.Sales.Application.Internal.OutboundServices;
using Acme.Bakery.Platform.Sales.Domain.Model.Aggregates;
using Acme.Bakery.Platform.Sales.Domain.Model.Commands;
using Acme.Bakery.Platform.Sales.Domain.Model.Errors;
using Acme.Bakery.Platform.Sales.Domain.Repositories;
using Acme.Bakery.Platform.Shared.Application.Model;
using Acme.Bakery.Platform.Shared.Domain.Repositories;

namespace Acme.Bakery.Platform.Sales.Application.Internal.CommandServices;

public class OrderCommandService(
    IOrderRepository orderRepository,
    ExternalInventoryService externalInventoryService,
    IUnitOfWork unitOfWork) : IOrderCommandService
{
    public async Task<Result<Order>> Handle(CreateOrderCommand command, CancellationToken cancellationToken)
    {
        // 1. Validaciones simples
        if (string.IsNullOrWhiteSpace(command.CustomerName))
            return Result<Order>.Failure(SalesErrors.CustomerNameRequired);

        if (command.Quantity < 1)
            return Result<Order>.Failure(SalesErrors.InvalidQuantity);

        if (command.PickupDate.Date < DateTime.Today)
            return Result<Order>.Failure(SalesErrors.PickupDateInPast);

        // 2. Preguntar a Inventory (vía ACL) si el producto existe y su precio
        var unitPrice = await externalInventoryService.FetchProductPriceAsync(command.ProductId, cancellationToken);
        if (unitPrice is null)
            return Result<Order>.Failure(SalesErrors.ProductNotFound);

        // 3. Reservar stock (Inventory lo descuenta, pero todavía NO guarda)
        var reserved = await externalInventoryService.ReserveStockAsync(
            command.ProductId, command.Quantity, cancellationToken);
        if (!reserved)
            return Result<Order>.Failure(SalesErrors.InsufficientStock);

        // 4. Crear el pedido y guardar TODO (pedido + stock) en una sola transacción
        var order = new Order(command.CustomerName, command.ProductId, command.Quantity, unitPrice.Value,
            command.PickupDate);
        await orderRepository.AddAsync(order, cancellationToken);
        await unitOfWork.CompleteAsync(cancellationToken);

        return Result<Order>.Success(order);
    }
}
```

**`Sales/Application/Internal/QueryServices/OrderQueryService.cs`**

```csharp
using Acme.Bakery.Platform.Sales.Application.QueryServices;
using Acme.Bakery.Platform.Sales.Domain.Model.Aggregates;
using Acme.Bakery.Platform.Sales.Domain.Model.Queries;
using Acme.Bakery.Platform.Sales.Domain.Repositories;

namespace Acme.Bakery.Platform.Sales.Application.Internal.QueryServices;

public class OrderQueryService(IOrderRepository orderRepository) : IOrderQueryService
{
    public async Task<Order?> Handle(GetOrderByIdQuery query, CancellationToken cancellationToken)
    {
        return await orderRepository.FindByIdAsync(query.OrderId, cancellationToken);
    }

    public async Task<IEnumerable<Order>> Handle(GetAllOrdersQuery query, CancellationToken cancellationToken)
    {
        return await orderRepository.ListAsync(cancellationToken);
    }
}
```

#### 7.3 Infrastructure

**`Sales/Infrastructure/Persistence/EFC/Configuration/Extensions/ModelBuilderExtensions.cs`**

```csharp
using Acme.Bakery.Platform.Sales.Domain.Model.Aggregates;
using Microsoft.EntityFrameworkCore;

namespace Acme.Bakery.Platform.Sales.Infrastructure.Persistence.EFC.Configuration.Extensions;

public static class ModelBuilderExtensions
{
    public static void ApplySalesConfiguration(this ModelBuilder builder)
    {
        builder.Entity<Order>().ToTable("orders");
        builder.Entity<Order>().HasKey(o => o.Id);
        builder.Entity<Order>().Property(o => o.Id).IsRequired().ValueGeneratedOnAdd();
        builder.Entity<Order>().Property(o => o.CustomerName).IsRequired().HasMaxLength(100);
        builder.Entity<Order>().Property(o => o.ProductId).IsRequired();
        builder.Entity<Order>().Property(o => o.Quantity).IsRequired();
        builder.Entity<Order>().Property(o => o.UnitPrice).IsRequired().HasPrecision(10, 2);
        builder.Entity<Order>().Property(o => o.TotalAmount).IsRequired().HasPrecision(10, 2);
        builder.Entity<Order>().Property(o => o.PickupDate).IsRequired();
        builder.Entity<Order>().Property(o => o.Status).IsRequired()
            .HasConversion<string>().HasMaxLength(20);
    }
}
```

**`Sales/Infrastructure/Persistence/EFC/Repositories/OrderRepository.cs`**

```csharp
using Acme.Bakery.Platform.Sales.Domain.Model.Aggregates;
using Acme.Bakery.Platform.Sales.Domain.Repositories;
using Acme.Bakery.Platform.Shared.Infrastructure.Persistence.EFC.Configuration;
using Acme.Bakery.Platform.Shared.Infrastructure.Persistence.EFC.Repositories;

namespace Acme.Bakery.Platform.Sales.Infrastructure.Persistence.EFC.Repositories;

public class OrderRepository(AppDbContext context) : BaseRepository<Order>(context), IOrderRepository;
```

#### 7.4 Interfaces (REST)

```csharp
// Sales/Interfaces/Rest/Resources/CreateOrderResource.cs
namespace Acme.Bakery.Platform.Sales.Interfaces.Rest.Resources;
public record CreateOrderResource(string CustomerName, int ProductId, int Quantity, DateTime PickupDate);

// Sales/Interfaces/Rest/Resources/OrderResource.cs
namespace Acme.Bakery.Platform.Sales.Interfaces.Rest.Resources;
public record OrderResource(int Id, string CustomerName, int ProductId, int Quantity, decimal UnitPrice,
    decimal TotalAmount, DateTime PickupDate, string Status, DateTime? CreatedAt, DateTime? UpdatedAt);
```

```csharp
// Sales/Interfaces/Rest/Transform/CreateOrderCommandFromResourceAssembler.cs
using Acme.Bakery.Platform.Sales.Domain.Model.Commands;
using Acme.Bakery.Platform.Sales.Interfaces.Rest.Resources;

namespace Acme.Bakery.Platform.Sales.Interfaces.Rest.Transform;

public static class CreateOrderCommandFromResourceAssembler
{
    public static CreateOrderCommand ToCommandFromResource(CreateOrderResource resource)
    {
        return new CreateOrderCommand(resource.CustomerName, resource.ProductId, resource.Quantity,
            resource.PickupDate);
    }
}
```

```csharp
// Sales/Interfaces/Rest/Transform/OrderResourceFromEntityAssembler.cs
using Acme.Bakery.Platform.Sales.Domain.Model.Aggregates;
using Acme.Bakery.Platform.Sales.Interfaces.Rest.Resources;

namespace Acme.Bakery.Platform.Sales.Interfaces.Rest.Transform;

public static class OrderResourceFromEntityAssembler
{
    public static OrderResource ToResourceFromEntity(Order entity)
    {
        return new OrderResource(
            entity.Id,
            entity.CustomerName,
            entity.ProductId,
            entity.Quantity,
            entity.UnitPrice,
            entity.TotalAmount,
            entity.PickupDate,
            entity.Status.ToString().ToUpperInvariant(),
            entity.CreatedAt,
            entity.UpdatedAt);
    }
}
```

**`Sales/Interfaces/Rest/OrdersController.cs`**

```csharp
using System.Net.Mime;
using Acme.Bakery.Platform.Sales.Application.CommandServices;
using Acme.Bakery.Platform.Sales.Application.QueryServices;
using Acme.Bakery.Platform.Sales.Domain.Model.Queries;
using Acme.Bakery.Platform.Sales.Interfaces.Rest.Resources;
using Acme.Bakery.Platform.Sales.Interfaces.Rest.Transform;
using Acme.Bakery.Platform.Shared.Interfaces.Rest.Transform;
using Microsoft.AspNetCore.Mvc;

namespace Acme.Bakery.Platform.Sales.Interfaces.Rest;

[ApiController]
[Route("api/v1/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
[Tags("Orders")]
public class OrdersController(
    IOrderCommandService orderCommandService,
    IOrderQueryService orderQueryService) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(OrderResource), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderResource resource,
        CancellationToken cancellationToken)
    {
        var command = CreateOrderCommandFromResourceAssembler.ToCommandFromResource(resource);
        var result = await orderCommandService.Handle(command, cancellationToken);

        if (result.IsFailure)
            return ErrorActionResultAssembler.ToActionResultFromError(this, result.Error!);

        var orderResource = OrderResourceFromEntityAssembler.ToResourceFromEntity(result.Value!);
        return CreatedAtAction(nameof(GetOrderById), new { orderId = orderResource.Id }, orderResource);
    }

    [HttpGet("{orderId:int}")]
    [ProducesResponseType(typeof(OrderResource), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrderById(int orderId, CancellationToken cancellationToken)
    {
        var order = await orderQueryService.Handle(new GetOrderByIdQuery(orderId), cancellationToken);
        if (order is null) return NotFound();
        return Ok(OrderResourceFromEntityAssembler.ToResourceFromEntity(order));
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<OrderResource>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllOrders(CancellationToken cancellationToken)
    {
        var orders = await orderQueryService.Handle(new GetAllOrdersQuery(), cancellationToken);
        return Ok(orders.Select(OrderResourceFromEntityAssembler.ToResourceFromEntity));
    }
}
```

---

### Paso 8: `Program.cs` (inyección de dependencias y pipeline)

```csharp
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

var builder = WebApplication.CreateBuilder(args);

// ---------- Web API ----------
builder.Services.AddRouting(options => options.LowercaseUrls = true);
builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

// ---------- Base de datos (MySQL + EF Core) ----------
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                       ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseMySQL(connectionString)
        .UseSnakeCaseNamingConvention() // CustomerName -> customer_name, CreatedAt -> created_at
        .EnableDetailedErrors();

    if (builder.Environment.IsDevelopment())
        options.EnableSensitiveDataLogging();
});

// ---------- Inyección de dependencias ----------
// Shared
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Inventory
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IProductCommandService, ProductCommandService>();
builder.Services.AddScoped<IProductQueryService, ProductQueryService>();
builder.Services.AddScoped<IInventoryContextFacade, InventoryContextFacade>();

// Sales
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IOrderCommandService, OrderCommandService>();
builder.Services.AddScoped<IOrderQueryService, OrderQueryService>();
builder.Services.AddScoped<ExternalInventoryService>();

var app = builder.Build();

// ---------- Crear BD y tablas si no existen ----------
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

app.UseHttpsRedirection();
app.MapControllers();

app.Run();
```

> **¿Por qué `AddScoped`?** El `AppDbContext` es *scoped* (una instancia por petición HTTP). Como `ProductCommandService`, `OrderCommandService` y `UnitOfWork` reciben **el mismo** `AppDbContext` dentro de una petición, el `SaveChanges` de Sales guarda también el stock modificado por Inventory. Eso es el **Unit of Work** en acción.

---

### Paso 9: Compilar y ejecutar

**En la Mac de la universidad** (o cualquier PC sin otros proyectos configurados):

```bash
# Desde la carpeta raíz (donde está bakery-platform.sln)
dotnet build

# Ejecutar
dotnet run --project Acme.Bakery.Platform --launch-profile http
```

**En tu PC con Windows** (PowerShell), si tienes la variable `ConnectionStrings__DefaultConnection` de otro proyecto (por ejemplo, WashTrack):

```powershell
# 1. Quitar la variable SOLO en esta terminal (el otro proyecto no se ve afectado)
Remove-Item Env:ConnectionStrings__DefaultConnection

# 2. Ejecutar
dotnet run --project Acme.Bakery.Platform --launch-profile http
```

> ¿Por qué? En .NET las variables de entorno **tienen prioridad** sobre `appsettings.json`. Si esa variable existe, la API intenta conectarse a la base de datos del otro proyecto en vez de a tu MySQL local, falla y se cierra.
> Para saber si la tienes: `$env:ConnectionStrings__DefaultConnection`. Si no imprime nada, no la tienes.

Espera a ver en la terminal:

```
Now listening on: http://localhost:5054
```

Abre en el navegador (o con `open <url>` en Mac, o `start <url>` en Windows):

- Documentación Scalar: <http://localhost:5054/scalar>
- Especificación OpenAPI: <http://localhost:5054/openapi/v1.json>

> El puerto (`5054`) viene de `Properties/launchSettings.json`. Si tu proyecto generó otro puerto, usa ese.
>
> Si el navegador dice **`ERR_CONNECTION_REFUSED`** ("localhost ha rechazado la conexión"), **la API no está corriendo**. Revisa la terminal: lo más común es que se haya cerrado por un error de conexión a MySQL (ver [Problemas comunes](#3-problemas-comunes)).
>
> Para detener la API: `Ctrl + C` en la terminal donde corre (igual en Mac).

### Paso 10: Probar la API

**Opción A: Scalar** (<http://localhost:5054/scalar>): elige el endpoint, haz clic en *Test Request*, pega el JSON y envía.

**Opción B: archivo `.http`**: abre `Acme.Bakery.Platform/Acme.Bakery.Platform.http` en Rider o Visual Studio y haz clic en ▶ al lado de cada petición.

**Opción C: PowerShell**:

```powershell
$api = "http://localhost:5054/api/v1"

# Crear productos
Invoke-RestMethod -Method Post -Uri "$api/products" -ContentType "application/json" `
  -Body '{ "name": "Pan Frances", "price": 0.50, "stock": 100, "category": "BREAD" }'

Invoke-RestMethod -Method Post -Uri "$api/products" -ContentType "application/json" `
  -Body '{ "name": "Torta de Chocolate", "price": 45.00, "stock": 3, "category": "CAKE" }'

# Listar productos
Invoke-RestMethod "$api/products"

# Crear un pedido (la fecha debe ser hoy o futura)
$fecha = (Get-Date).AddDays(1).ToString("yyyy-MM-dd")
Invoke-RestMethod -Method Post -Uri "$api/orders" -ContentType "application/json" `
  -Body "{ `"customerName`": `"Ana Torres`", `"productId`": 2, `"quantity`": 2, `"pickupDate`": `"$fecha`" }"

# Ver que el stock bajó de 3 a 1
Invoke-RestMethod "$api/products/2"
```

> Para ver el código HTTP de un error usa `curl.exe -i ...` (con `.exe`; en PowerShell `curl` a secas es un alias de `Invoke-WebRequest`).

**Resultados esperados** (probados con la solución):

| # | Petición                                                    | Esperado |
|---|-------------------------------------------------------------|----------|
| 1 | POST producto válido                                        | `201` + header `Location: http://localhost:5054/api/v1/products/1` |
| 2 | POST producto con nombre repetido                           | `409` `Inventory.NameAlreadyExists` |
| 3 | POST producto con `price: 0`                                | `400` `Inventory.InvalidPrice` |
| 4 | POST producto con `category: "PIZZA"`                       | `400` `Inventory.InvalidCategory` |
| 5 | GET `/products/999`                                         | `404` |
| 6 | POST pedido de 2 tortas (stock 3)                           | `201`, `totalAmount: 90.00`, `status: "PENDING"` |
| 7 | GET `/products/2`                                           | `stock: 1` (se descontó) |
| 8 | POST pedido de 5 tortas (stock 1)                           | `409` `Sales.InsufficientStock` |
| 9 | POST pedido con `productId: 999`                            | `404` `Sales.ProductNotFound` |
| 10| POST pedido con fecha pasada                                | `400` `Sales.PickupDateInPast` |
| 11| POST pedido con `quantity: 0`                               | `400` `Sales.InvalidQuantity` |

Ejemplo de respuesta de error (ProblemDetails):

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.10",
  "title": "Sales.InsufficientStock",
  "status": 409,
  "detail": "There is not enough stock for the requested quantity.",
  "traceId": "00-5bae4f..."
}
```

### Paso 11: Verificar en MySQL Workbench

1. En Workbench, abre la conexión local (`root` / `1234567`).
2. En el panel **Schemas** haz clic en el ícono de refrescar (🔄): aparece `acme_bakery_db`.
3. Abre una pestaña de SQL (**File → New Query Tab**) y ejecuta (⚡ o `Ctrl + Shift + Enter`):

```sql
USE acme_bakery_db;

SHOW TABLES;
DESCRIBE products;
DESCRIBE orders;

SELECT * FROM products;
SELECT * FROM orders;
```

Deberías ver las tablas `products` y `orders` con columnas en snake_case (`customer_name`, `total_amount`, `created_at`, …) y comprobar que el `stock` del producto bajó después de crear un pedido.

Para **empezar de cero** (borra todos los datos), ejecuta en Workbench:

```sql
DROP DATABASE acme_bakery_db;
-- Al volver a ejecutar la app, EnsureCreated() la vuelve a crear
```

> `EnsureCreated()` **no actualiza** tablas que ya existen. Si cambias una entidad (agregas un campo), borra la base de datos y vuelve a ejecutar.

---

## 3. Problemas comunes

| Síntoma | Causa | Solución |
|---|---|---|
| `Access denied for user ... '<otro host>'`: la app se conecta a **otro servidor** que no es localhost | Existe una **variable de entorno** `ConnectionStrings__DefaultConnection` (de otro proyecto). En .NET las variables de entorno **tienen prioridad** sobre `appsettings.json` | Verifica con `$env:ConnectionStrings__DefaultConnection`. Para esa terminal: `Remove-Item Env:ConnectionStrings__DefaultConnection`. O renombra la conexión en este proyecto (p. ej. `BakeryConnection`) en `appsettings.json` y en `Program.cs` |
| El navegador muestra `ERR_CONNECTION_REFUSED` en `localhost:5054/scalar` | La API no está corriendo (no se ejecutó o se cerró por un error) | Mira la terminal del `dotnet run`. Si el error es de conexión a MySQL, revisa la fila anterior y la siguiente |
| `Access denied for user 'root'@'localhost'` | Contraseña distinta | Prueba la conexión en Workbench con `1234567`; si tu MySQL usa otra, cambia el `password=` del `appsettings.json` |
| `Unable to connect to any of the specified MySQL hosts` | El servicio de MySQL está apagado | `Get-Service *mysql*` y luego `Start-Service <nombre>` (como administrador), o desde *services.msc* |
| `NU1202: ... no es compatible con net10.0` | Se instaló un paquete 11.x | Instala con `--version 10.0.x` (ver Paso 2) |
| `Address already in use` / puerto ocupado | Otra instancia corriendo | Cierra la otra ejecución o cambia el puerto en `launchSettings.json` |
| Agregaste un campo y no aparece en la tabla | `EnsureCreated` no migra | `DROP DATABASE acme_bakery_db;` y vuelve a ejecutar |
| `400` con `"errors": { "Name": ["The Name field is required."] }` | Falta un campo en el JSON (lo valida `[ApiController]` automáticamente) | Envía todos los campos del Resource |

---

## 4. Conceptos que debes saber explicar

| Concepto | Dónde está en este proyecto | En una frase |
|---|---|---|
| **Bounded Context** | `Inventory/`, `Sales/`, `Shared/` | Frontera donde un modelo tiene un significado único |
| **Aggregate Root** | `Product`, `Order` | Única puerta de entrada para modificar el estado (setters privados) |
| **Value Object / Enum** | `EProductCategory`, `EOrderStatus` | Valores sin identidad propia |
| **CQRS** | `*CommandService` vs `*QueryService` | Escribir y leer están separados |
| **Command / Query** | `CreateOrderCommand`, `GetOrderByIdQuery` | Records inmutables que expresan una intención |
| **Repository** | `IProductRepository` (Domain) / `ProductRepository` (Infra) | Abstrae el acceso a datos |
| **Unit of Work** | `IUnitOfWork.CompleteAsync()` | Un solo `SaveChanges` = una transacción |
| **Result pattern** | `Result<T>` + `InventoryErrors` / `SalesErrors` | Los errores esperados no son excepciones |
| **Assembler** | `*Assembler` en `Interfaces/Rest/Transform` | Convierte Resource ⇄ Command ⇄ Entity |
| **ACL / Facade** | `IInventoryContextFacade` + `ExternalInventoryService` | Sales usa Inventory sin conocer `Product` |
| **Interceptor** | `AuditableEntityInterceptor` | Llena `created_at` / `updated_at` automáticamente |
| **Fluent API** | `ModelBuilderExtensions` | Mapeo de tablas sin ensuciar las entidades |
| **Primary constructors** | `class ProductRepository(AppDbContext context)` | Inyección de dependencias sin código repetitivo |

**Flujo de un `POST /api/v1/orders`:**

```
HTTP JSON
  → CreateOrderResource                 (Interfaces/Rest/Resources)
  → CreateOrderCommandFromResourceAssembler
  → CreateOrderCommand                  (Domain/Model/Commands)
  → OrderCommandService.Handle()        (Application)
        ├─ validaciones → Result.Failure(SalesErrors.X) → 400/404/409
        ├─ ExternalInventoryService → IInventoryContextFacade → ProductQueryService / ProductCommandService
        ├─ new Order(...)               (Domain: calcula TotalAmount, Status = Pending)
        ├─ orderRepository.AddAsync()   (Infrastructure)
        └─ unitOfWork.CompleteAsync()   → AuditableEntityInterceptor → INSERT order + UPDATE product
  → Result.Success(order)
  → OrderResourceFromEntityAssembler
  → 201 Created + OrderResource
```

---

## 5. Diferencias con el proyecto del profesor

La versión completa de `learning-center-platform` (ver su `docs/study-guide.md`) agrega lo siguiente, que aquí se omitió para que sea simple:

| Del profesor | En esta práctica |
|---|---|
| **Cortex Mediator** con pipeline behaviors (logging, transacción, excepciones) | Los servicios se llaman directamente y hacen `CompleteAsync()` |
| **Strongly-typed IDs** (`record struct ProductId(int Value)`) + `StronglyTypedIdConverter` | `int Id` |
| **Domain Events** (`AddDomainEvent`, `DispatchDomainEventsInterceptor`) | No hay eventos |
| BC **IAM** con JWT + BCrypt y middleware de autorización | Sin autenticación |
| **Localización** (en/es) de mensajes de error | Mensajes fijos en inglés |
| `KebabCaseRouteNamingConvention` | `LowercaseUrls` (las rutas son de una palabra) |
| ADRs, diagramas PlantUML | No incluidos |
| — | **Sin Docker:** MySQL local + MySQL Workbench (indicación del profesor) |

Si en la PC piden alguno de estos puntos, revisa la sección correspondiente del `study-guide.md` del profesor.

---

## 6. Ejercicios extra para practicar

1. **Fácil:** agrega `GET /api/v1/products?category=CAKE` para filtrar por categoría (nuevo `GetAllProductsByCategoryQuery` + método en el repositorio).
2. **Fácil:** agrega `GET /api/v1/products/{productId}/orders` que liste los pedidos de un producto (nuevo controller en Sales, como `CategoryTutorialsController` del profe).
3. **Medio:** agrega `PATCH /api/v1/orders/{orderId}/deliver` que cambie el estado a `DELIVERED`. Regla: solo un pedido `PENDING` puede entregarse (si no, `409`). Implementa el método `Deliver()` dentro del aggregate `Order`.
4. **Medio:** regla nueva: un mismo cliente no puede tener dos pedidos del mismo producto para la misma fecha de recojo (`409`).
5. **Medio:** convierte `CustomerName` en un Value Object `CustomerName(string FirstName, string LastName)` mapeado con `OwnsOne` en EF Core.
6. **Difícil:** reemplaza `int Id` de `Product` por un strongly-typed ID `ProductId` como el `UserId` del profesor.
7. **Difícil:** cancelar un pedido (`PATCH /orders/{id}/cancel`) debe **devolver el stock** al producto a través del ACL (nuevo método en `IInventoryContextFacade`).
8. **Reto:** cambia `EnsureCreated()` por **migraciones**: `dotnet tool install --global dotnet-ef`, `dotnet add package Microsoft.EntityFrameworkCore.Design --version 10.0.15`, `dotnet ef migrations add InitialCreate`, `dotnet ef database update`.
