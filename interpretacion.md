# Interpretación del enunciado: Acme Bakery

Este documento enseña a **leer un enunciado redactado en párrafos** (como llega en la PC) y a **separarlo** en todo lo que hay que programar: bounded contexts, aggregates, atributos, reglas de negocio, commands, queries, endpoints y requisitos técnicos.

> Primero lee el enunciado completo (sección 1). Intenta tú mismo hacer la separación en una hoja y luego compárala con la sección 2.

---

## 1. Enunciado

**Acme Bakery** es una panadería que vende panes, tortas, pasteles y galletas. Actualmente registra sus productos y los pedidos de sus clientes en un cuaderno, lo que ha provocado que se acepten pedidos de productos que ya se agotaron. Por ello, la gerencia ha decidido implementar una plataforma y te ha encargado desarrollar el backend como un servicio web RESTful con ASP.NET Core sobre .NET 10, utilizando Entity Framework Core y una base de datos MySQL, y aplicando Domain-Driven Design. La solución debe llamarse `bakery-platform` y el proyecto `Acme.Bakery.Platform`.

El primer bounded context, **Inventory**, se encarga de gestionar los productos de la panadería. De cada producto se registra su nombre, que es obligatorio, no debe superar los 100 caracteres y no puede repetirse entre productos; su precio unitario en soles, que debe ser mayor que cero; la cantidad de unidades disponibles en stock, que nunca puede ser negativa; y su categoría, que únicamente puede ser BREAD, CAKE, PASTRY o COOKIE. La categoría debe aceptarse sin distinguir mayúsculas de minúsculas y almacenarse como texto. La plataforma debe permitir registrar un producto, consultar todos los productos y consultar un producto por su identificador.

El segundo bounded context, **Sales**, se encarga de los pedidos que los clientes recogen en tienda. Para registrar un pedido, el cliente indica su nombre, que es obligatorio y de hasta 100 caracteres; el identificador del producto que desea; la cantidad de unidades, que debe ser al menos una; y la fecha en que recogerá el pedido, la cual no puede ser anterior a la fecha actual. El producto solicitado debe existir y debe contar con stock suficiente para atender la cantidad pedida. El precio unitario no lo envía el cliente: el sistema lo obtiene del producto al momento del registro y calcula el monto total multiplicando dicho precio por la cantidad. Todo pedido nuevo se registra con estado PENDING; más adelante podrá pasar a DELIVERED. Al registrarse un pedido, el stock del producto debe disminuir en la cantidad solicitada, y ambos cambios deben guardarse en una misma operación, de modo que nunca quede un pedido registrado sin su descuento de stock ni viceversa. La plataforma debe permitir registrar un pedido, consultar todos los pedidos y consultar un pedido por su identificador.

Para mantener la independencia entre ambos contextos, Sales no debe hacer referencia directa a las clases del dominio de Inventory; toda consulta o modificación que necesite sobre los productos debe realizarse a través de una fachada que Inventory exponga para otros contextos, usando únicamente tipos primitivos.

Tanto los productos como los pedidos deben registrar automáticamente su fecha de creación y de última actualización, sin que el cliente envíe estos valores. Las tablas de la base de datos deben nombrarse en plural y sus columnas en snake_case, y los montos deben almacenarse con dos decimales. La base de datos debe llamarse `acme_bakery_db`; debe usarse el servidor MySQL local con usuario `root` y contraseña `1234567`, y tanto la base de datos como las tablas deben crearse automáticamente al iniciar la aplicación. Los endpoints deben usar el prefijo `/api/v1`, responder con los códigos HTTP adecuados para cada caso (creación, consulta, datos inválidos, recurso inexistente y conflicto con el estado actual) y devolver los errores en formato ProblemDetails. Finalmente, la API debe estar documentada con OpenAPI y poder explorarse con Scalar.

---

## 2. Interpretación paso a paso

### Paso 1: Leer y marcar el texto

Lee el enunciado **dos veces**. En la segunda lectura marca con colores (o con símbolos en tu hoja):

| Qué buscar | Qué significa en el código | Ejemplos en el enunciado |
|---|---|---|
| **Sustantivos principales** (las "cosas" que se registran) | Aggregates / entidades | *producto*, *pedido* |
| **"De cada X se registra…"** | Atributos del aggregate | *nombre, precio, stock, categoría* |
| **"únicamente puede ser A, B, C"** | `enum` | *BREAD, CAKE, PASTRY, COOKIE* · *PENDING, DELIVERED* |
| **"obligatorio", "mayor que", "al menos", "no puede", "nunca"** | Reglas de validación → `400` | *debe ser mayor que cero*, *al menos una* |
| **"no puede repetirse"** | Unicidad → índice único + `409` | *no puede repetirse entre productos* |
| **"debe existir"** | Búsqueda previa → `404` | *El producto solicitado debe existir* |
| **"suficiente", "disponible"** (estado actual) | Conflicto → `409` | *stock suficiente* |
| **"el sistema calcula / obtiene / asigna"** | **No va** en el Resource de entrada | *precio unitario*, *monto total*, *estado*, *fechas de auditoría* |
| **Verbos "registrar", "crear"** | Command + `POST` → `201` | *registrar un producto* |
| **Verbos "consultar", "listar", "obtener"** | Query + `GET` → `200` | *consultar todos los productos* |
| **"en una misma operación / transacción"** | Unit of Work: un solo `SaveChanges` | *ambos cambios deben guardarse en una misma operación* |
| **"no debe hacer referencia directa… a través de una fachada"** | Anti-Corruption Layer | *fachada que Inventory exponga* |
| **Nombres técnicos** (tablas, BD, rutas, formatos) | Configuración / infraestructura | *plural*, *snake_case*, `/api/v1`, *ProblemDetails* |

---

### Paso 2: Identificar los bounded contexts

El propio texto los nombra:

| Bounded context | Frase del enunciado | Responsabilidad |
|---|---|---|
| **Inventory** | *"se encarga de gestionar los productos"* | Productos y su stock |
| **Sales** | *"se encarga de los pedidos que los clientes recogen en tienda"* | Pedidos |
| **Shared** | (no se menciona, lo agregas tú) | Código común: `Error`, `Result<T>`, repositorio base, Unit of Work, `AppDbContext`, interceptor de auditoría |

> **Pista:** si el enunciado no nombra los contextos, agrupa los sustantivos por "quién es dueño de qué". El stock pertenece al producto, así que va en Inventory aunque Sales lo modifique.

Cada contexto lleva las 4 capas del profesor: `Domain`, `Application`, `Infrastructure`, `Interfaces`.

---

### Paso 3: Aggregates y atributos

#### `Product` (Inventory)

| Frase del enunciado | Atributo | Tipo C# | Por qué ese tipo |
|---|---|---|---|
| *"consultar un producto por su identificador"* | `Id` | `int` | Autogenerado por la BD |
| *"su nombre… no debe superar los 100 caracteres"* | `Name` | `string` | `HasMaxLength(100)` |
| *"su precio unitario en soles"* | `Price` | `decimal` | Dinero **siempre** `decimal`, nunca `double` |
| *"la cantidad de unidades disponibles en stock"* | `Stock` | `int` | Unidades enteras |
| *"únicamente puede ser BREAD, CAKE, PASTRY o COOKIE"* | `Category` | `EProductCategory` (enum) | Valores cerrados |
| *"fecha de creación"* | `CreatedAt` | `DateTime?` | La llena el interceptor |
| *"fecha… de última actualización"* | `UpdatedAt` | `DateTime?` | La llena el interceptor |

#### `Order` (Sales)

| Frase del enunciado | Atributo | Tipo C# | Lo envía el cliente? |
|---|---|---|---|
| *"consultar un pedido por su identificador"* | `Id` | `int` | No (autogenerado) |
| *"el cliente indica su nombre"* | `CustomerName` | `string` | **Sí** |
| *"el identificador del producto que desea"* | `ProductId` | `int` | **Sí** |
| *"la cantidad de unidades"* | `Quantity` | `int` | **Sí** |
| *"la fecha en que recogerá el pedido"* | `PickupDate` | `DateTime` | **Sí** |
| *"el sistema lo obtiene del producto"* | `UnitPrice` | `decimal` | No (viene de Inventory) |
| *"calcula el monto total multiplicando…"* | `TotalAmount` | `decimal` | No (se calcula en el constructor) |
| *"se registra con estado PENDING… podrá pasar a DELIVERED"* | `Status` | `EOrderStatus` (enum) | No (siempre `Pending` al crear) |
| *"fecha de creación… última actualización"* | `CreatedAt`, `UpdatedAt` | `DateTime?` | No |

> **Clave:** `Order` guarda **solo `ProductId` (int)**, no una propiedad `Product`. Si pusieras `public Product Product`, Sales dependería del dominio de Inventory y romperías la regla del ACL.

---

### Paso 4: Reglas de negocio → dónde se programan → código HTTP

| # | Frase del enunciado | Regla | Dónde se valida | HTTP |
|---|---|---|---|---|
| RN-01 | *"su nombre, que es obligatorio"* | `Name` no vacío | `ProductCommandService` | 400 |
| RN-02 | *"no puede repetirse entre productos"* | Nombre único | `productRepository.ExistsByNameAsync` + índice único | **409** |
| RN-03 | *"debe ser mayor que cero"* | `Price > 0` | `ProductCommandService` | 400 |
| RN-04 | *"nunca puede ser negativa"* | `Stock >= 0` | `ProductCommandService` | 400 |
| RN-05 | *"únicamente puede ser BREAD, CAKE, PASTRY o COOKIE… sin distinguir mayúsculas"* | Categoría válida | `Enum.TryParse(..., ignoreCase: true)` | 400 |
| RN-06 | *"almacenarse como texto"* | Enum guardado como string | `HasConversion<string>()` | — |
| RN-07 | *"su nombre, que es obligatorio y de hasta 100 caracteres"* | `CustomerName` no vacío | `OrderCommandService` | 400 |
| RN-08 | *"debe ser al menos una"* | `Quantity >= 1` | `OrderCommandService` | 400 |
| RN-09 | *"no puede ser anterior a la fecha actual"* | `PickupDate.Date >= Today` | `OrderCommandService` | 400 |
| RN-10 | *"El producto solicitado debe existir"* | Producto existe | ACL → `FetchProductPriceByIdAsync` devuelve `null` | **404** |
| RN-11 | *"debe contar con stock suficiente"* | `Stock >= Quantity` | ACL → `DecreaseProductStockAsync` devuelve `false` | **409** |
| RN-12 | *"el sistema lo obtiene del producto"* | `UnitPrice` viene de Inventory | `OrderCommandService` vía ACL | — |
| RN-13 | *"calcula el monto total multiplicando dicho precio por la cantidad"* | `TotalAmount = UnitPrice * Quantity` | Constructor de `Order` | — |
| RN-14 | *"Todo pedido nuevo se registra con estado PENDING"* | Estado inicial | Constructor de `Order` | — |
| RN-15 | *"el stock del producto debe disminuir en la cantidad solicitada"* | Descontar stock | `Product.DecreaseStock()` | — |
| RN-16 | *"ambos cambios deben guardarse en una misma operación"* | Una sola transacción | Un único `unitOfWork.CompleteAsync()` en `OrderCommandService` | — |
| RN-17 | *"registrar automáticamente su fecha de creación y de última actualización"* | Auditoría | `AuditableEntityInterceptor` | — |

**¿Cómo decidir entre 400, 404 y 409?**

- **400 Bad Request:** el dato **por sí solo** está mal (vacío, negativo, fecha pasada, categoría inexistente). No necesitas consultar la BD para saberlo.
- **404 Not Found:** el dato tiene buen formato, pero **lo que referencia no existe** (`productId: 999`).
- **409 Conflict:** el dato está bien, pero **choca con el estado actual** del sistema (nombre ya usado, stock insuficiente).

---

### Paso 5: Commands y queries (CQRS)

Cada verbo del enunciado se convierte en un command (escribe) o una query (lee):

| Frase del enunciado | Tipo | Record | Servicio |
|---|---|---|---|
| *"registrar un producto"* | Command | `CreateProductCommand(Name, Price, Stock, Category)` | `IProductCommandService` |
| *"el stock del producto debe disminuir"* | Command | `DecreaseProductStockCommand(ProductId, Quantity)` | `IProductCommandService` |
| *"consultar todos los productos"* | Query | `GetAllProductsQuery` | `IProductQueryService` |
| *"consultar un producto por su identificador"* | Query | `GetProductByIdQuery(ProductId)` | `IProductQueryService` |
| *"registrar un pedido"* | Command | `CreateOrderCommand(CustomerName, ProductId, Quantity, PickupDate)` | `IOrderCommandService` |
| *"consultar todos los pedidos"* | Query | `GetAllOrdersQuery` | `IOrderQueryService` |
| *"consultar un pedido por su identificador"* | Query | `GetOrderByIdQuery(OrderId)` | `IOrderQueryService` |

> `CreateOrderCommand` **no tiene** `UnitPrice`, `TotalAmount` ni `Status`, porque el enunciado dice que el cliente no los envía.

---

### Paso 6: Endpoints

Regla general: **recurso en plural**, `POST` para registrar y `GET` para consultar, y el prefijo `/api/v1` que pide el enunciado.

| Método | Ruta | Command / Query | Respuestas |
|---|---|---|---|
| `POST` | `/api/v1/products` | `CreateProductCommand` | 201 · 400 · 409 |
| `GET` | `/api/v1/products` | `GetAllProductsQuery` | 200 |
| `GET` | `/api/v1/products/{productId}` | `GetProductByIdQuery` | 200 · 404 |
| `POST` | `/api/v1/orders` | `CreateOrderCommand` | 201 · 400 · 404 · 409 |
| `GET` | `/api/v1/orders` | `GetAllOrdersQuery` | 200 |
| `GET` | `/api/v1/orders/{orderId}` | `GetOrderByIdQuery` | 200 · 404 |

**Resources** (lo que entra y sale por HTTP):

| Resource | Campos | Por qué |
|---|---|---|
| `CreateProductResource` | `Name, Price, Stock, Category` | Lo que el cliente envía |
| `ProductResource` | `Id, Name, Price, Stock, Category, CreatedAt, UpdatedAt` | Lo que se devuelve |
| `CreateOrderResource` | `CustomerName, ProductId, Quantity, PickupDate` | **Solo** lo que *"el cliente indica"* |
| `OrderResource` | todos los atributos de `Order` | Lo que se devuelve |

---

### Paso 7: Integración entre contextos (ACL)

Frase: *"Sales no debe hacer referencia directa a las clases del dominio de Inventory; toda consulta o modificación… a través de una fachada que Inventory exponga… usando únicamente tipos primitivos."*

Se separa en 3 piezas:

| Pieza | Archivo | Qué hace |
|---|---|---|
| **Contrato** que Inventory publica | `Inventory/Interfaces/Acl/IInventoryContextFacade.cs` | `Task<decimal?> FetchProductPriceByIdAsync(int)` y `Task<bool> DecreaseProductStockAsync(int, int)`: solo primitivos |
| **Implementación** dentro de Inventory | `Inventory/Application/Acl/InventoryContextFacade.cs` | Usa los servicios de comandos y queries de Inventory |
| **Consumidor** en Sales | `Sales/Application/Internal/OutboundServices/ExternalInventoryService.cs` | Único lugar de Sales que conoce la fachada |

¿Qué necesita Sales de Inventory? Léelo del texto:
- *"debe existir"* + *"lo obtiene del producto"* → **un solo método** que devuelve el precio, o `null` si no existe. Con eso cubres RN-10 y RN-12 en una llamada.
- *"stock suficiente"* + *"debe disminuir"* → **un método** que intenta descontar y devuelve `true` o `false`. Cubre RN-11 y RN-15.

---

### Paso 8: Requisitos técnicos → decisión de implementación

| Frase del enunciado | Implementación |
|---|---|
| *"ASP.NET Core sobre .NET 10"* | `dotnet new web -f net10.0`, paquetes en versión **10.x** |
| *"Entity Framework Core y una base de datos MySQL"* | `MySql.EntityFrameworkCore` → `options.UseMySQL(...)` |
| *"servidor MySQL local con usuario root y contraseña 1234567"* | `appsettings.json`: `server=localhost;port=3306;user=root;password=1234567;database=acme_bakery_db;` |
| *"crearse automáticamente al iniciar la aplicación"* | `context.Database.EnsureCreated()` en `Program.cs` |
| *"tablas… en plural"* | `ToTable("products")`, `ToTable("orders")` |
| *"columnas en snake_case"* | `EFCore.NamingConventions` → `.UseSnakeCaseNamingConvention()` |
| *"montos… con dos decimales"* | `HasPrecision(10, 2)` en `Price`, `UnitPrice`, `TotalAmount` |
| *"registrar automáticamente su fecha de creación y de última actualización"* | `IAuditableEntity` + `AuditableEntityInterceptor` |
| *"prefijo /api/v1"* | `[Route("api/v1/[controller]")]` + `LowercaseUrls = true` |
| *"códigos HTTP adecuados"* | `Result<T>` + catálogo `InventoryErrors` / `SalesErrors` + `ErrorActionResultAssembler` |
| *"errores en formato ProblemDetails"* | `controller.Problem(...)` + `AddProblemDetails()` |
| *"documentada con OpenAPI y… Scalar"* | `AddOpenApi()`, `MapOpenApi()`, `MapScalarApiReference()` |
| *"aplicando Domain-Driven Design"* | Bounded contexts + capas + aggregates con setters privados + Repository + Unit of Work |

---

### Paso 9: Supuestos (lo que el enunciado no dice)

Un enunciado nunca dice todo. Si tomas una decisión, **déjala escrita** (en el README o como comentario):

| Duda | Decisión tomada | Razón |
|---|---|---|
| ¿Se puede recoger **hoy**? | Sí | *"no puede ser **anterior** a la fecha actual"*: hoy no es anterior |
| ¿`" Pan "` y `"Pan"` son el mismo nombre? | Sí, se aplica `Trim()` antes de validar y guardar | Evita duplicados con espacios |
| ¿`"cake"` es válido? | Sí, y se devuelve como `"CAKE"` | *"sin distinguir mayúsculas de minúsculas"* |
| ¿Hay endpoint para pasar a `DELIVERED`? | No | El texto dice *"más adelante podrá pasar"*, pero no pide el endpoint. Solo se crea el enum |
| ¿Qué devuelve el error 404 al pedir un producto inexistente en un pedido? | `Sales.ProductNotFound` | El error pertenece al contexto que lo detecta |
| ¿Se guarda el precio en el pedido o se consulta siempre? | Se **guarda** (`UnitPrice`) | *"lo obtiene… **al momento del registro**"*: si luego cambia el precio, el pedido conserva el original |

---

### Paso 10: Lista de archivos a crear (en orden)

Con todo lo anterior ya sabes exactamente qué programar:

```
Shared/
  Domain/Model/Errors/Error.cs                         ← ErrorType + Error
  Application/Model/Result.cs                          ← Result<T>
  Domain/Model/Entities/IAuditableEntity.cs
  Domain/Repositories/IBaseRepository.cs, IUnitOfWork.cs
  Infrastructure/Persistence/EFC/Configuration/AppDbContext.cs
  Infrastructure/Persistence/EFC/Interceptors/AuditableEntityInterceptor.cs
  Infrastructure/Persistence/EFC/Repositories/BaseRepository.cs, UnitOfWork.cs
  Interfaces/Rest/Transform/ErrorActionResultAssembler.cs

Inventory/
  Domain/Model/ValueObjects/EProductCategory.cs        ← Paso 3
  Domain/Model/Aggregates/Product.cs                   ← Paso 3, RN-15
  Domain/Model/Commands/CreateProductCommand.cs, DecreaseProductStockCommand.cs   ← Paso 5
  Domain/Model/Queries/GetProductByIdQuery.cs, GetAllProductsQuery.cs
  Domain/Model/Errors/InventoryErrors.cs               ← Paso 4
  Domain/Repositories/IProductRepository.cs            ← ExistsByNameAsync (RN-02)
  Application/CommandServices/IProductCommandService.cs
  Application/QueryServices/IProductQueryService.cs
  Application/Internal/CommandServices/ProductCommandService.cs   ← RN-01 a RN-05
  Application/Internal/QueryServices/ProductQueryService.cs
  Interfaces/Acl/IInventoryContextFacade.cs            ← Paso 7
  Application/Acl/InventoryContextFacade.cs
  Infrastructure/Persistence/EFC/Configuration/Extensions/ModelBuilderExtensions.cs   ← Paso 8
  Infrastructure/Persistence/EFC/Repositories/ProductRepository.cs
  Interfaces/Rest/Resources/CreateProductResource.cs, ProductResource.cs          ← Paso 6
  Interfaces/Rest/Transform/CreateProductCommandFromResourceAssembler.cs, ProductResourceFromEntityAssembler.cs
  Interfaces/Rest/ProductsController.cs

Sales/
  Domain/Model/ValueObjects/EOrderStatus.cs
  Domain/Model/Aggregates/Order.cs                     ← RN-13, RN-14
  Domain/Model/Commands/CreateOrderCommand.cs
  Domain/Model/Queries/GetOrderByIdQuery.cs, GetAllOrdersQuery.cs
  Domain/Model/Errors/SalesErrors.cs
  Domain/Repositories/IOrderRepository.cs
  Application/CommandServices/IOrderCommandService.cs
  Application/QueryServices/IOrderQueryService.cs
  Application/Internal/OutboundServices/ExternalInventoryService.cs   ← Paso 7
  Application/Internal/CommandServices/OrderCommandService.cs        ← RN-07 a RN-12, RN-16
  Application/Internal/QueryServices/OrderQueryService.cs
  Infrastructure/Persistence/EFC/Configuration/Extensions/ModelBuilderExtensions.cs
  Infrastructure/Persistence/EFC/Repositories/OrderRepository.cs
  Interfaces/Rest/Resources/CreateOrderResource.cs, OrderResource.cs
  Interfaces/Rest/Transform/CreateOrderCommandFromResourceAssembler.cs, OrderResourceFromEntityAssembler.cs
  Interfaces/Rest/OrdersController.cs

Program.cs                                             ← DbContext, DI (AddScoped), EnsureCreated, Scalar
appsettings.json                                       ← cadena de conexión
```

El código de cada archivo está en el [README.md](README.md), pasos 5 a 8.

---

### Paso 11: Checklist antes de entregar

Repasa el enunciado frase por frase y marca:

- [ ] La solución se llama `bakery-platform` y el proyecto `Acme.Bakery.Platform`.
- [ ] Existen las carpetas `Inventory/` y `Sales/`, cada una con `Domain`, `Application`, `Infrastructure` e `Interfaces`.
- [ ] `Product` y `Order` tienen setters privados y un constructor sin parámetros para EF Core.
- [ ] El nombre de producto duplicado devuelve **409**.
- [ ] Precio 0, stock negativo y categoría inválida devuelven **400**.
- [ ] `"cake"` se acepta y se guarda como texto.
- [ ] `CreateOrderResource` **no** tiene precio, total ni estado.
- [ ] El total es `precio × cantidad` y el estado inicial es `PENDING`.
- [ ] Fecha de recojo pasada → **400**. Cantidad 0 → **400**.
- [ ] Producto inexistente en un pedido → **404**. Stock insuficiente → **409**.
- [ ] Después de un pedido, el stock del producto bajó, con **un solo** `CompleteAsync()`.
- [ ] `Order` **no** tiene una propiedad `Product` y Sales **no** tiene `using ...Inventory.Domain...`.
- [ ] En Workbench, las tablas son `products` y `orders`, las columnas están en snake_case, los montos son `decimal(10,2)` y `created_at` / `updated_at` están llenos.
- [ ] Los errores salen en formato ProblemDetails (`type`, `title`, `status`, `detail`).
- [ ] `/scalar` abre la documentación.
- [ ] `dotnet build` termina con 0 errores.
