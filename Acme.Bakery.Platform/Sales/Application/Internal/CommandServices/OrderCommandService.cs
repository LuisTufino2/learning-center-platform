using Acme.Bakery.Platform.Sales.Application.CommandServices;
using Acme.Bakery.Platform.Sales.Application.Internal.OutboundServices;
using Acme.Bakery.Platform.Sales.Domain.Model.Aggregates;
using Acme.Bakery.Platform.Sales.Domain.Model.Commands;
using Acme.Bakery.Platform.Sales.Domain.Model.Errors;
using Acme.Bakery.Platform.Sales.Domain.Repositories;
using Acme.Bakery.Platform.Shared.Application.Model;
using Acme.Bakery.Platform.Shared.Domain.Repositories;

namespace Acme.Bakery.Platform.Sales.Application.Internal.CommandServices;

// Implementación de los comandos de pedidos: aplica las reglas de negocio de Sales.
public class OrderCommandService(
    IOrderRepository orderRepository,
    ExternalInventoryService externalInventoryService,
    IUnitOfWork unitOfWork) : IOrderCommandService
{
    // Registra un pedido: valida (400), verifica producto (404) y stock (409),
    // crea el pedido y guarda pedido + nuevo stock en una sola transacción (201)
    public async Task<Result<Order>> Handle(CreateOrderCommand command, CancellationToken cancellationToken)
    {
        // 1. Validaciones simples
        if (string.IsNullOrWhiteSpace(command.CustomerName))
            return Result<Order>.Failure(SalesErrors.CustomerNameRequired);

        if (command.Quantity < 1)
            return Result<Order>.Failure(SalesErrors.InvalidQuantity);

        if (command.PickupDate.Date < DateTime.Today)
            return Result<Order>.Failure(SalesErrors.PickupDateInPast);

        // 2. Preguntar a Inventory (a través del ACL) si el producto existe y cuál es su precio
        var unitPrice = await externalInventoryService.FetchProductPriceAsync(command.ProductId, cancellationToken);
        if (unitPrice is null)
            return Result<Order>.Failure(SalesErrors.ProductNotFound);

        // 3. Reservar stock (Inventory lo descuenta, pero todavía no lo guarda)
        var reserved = await externalInventoryService.ReserveStockAsync(
            command.ProductId, command.Quantity, cancellationToken);
        if (!reserved)
            return Result<Order>.Failure(SalesErrors.InsufficientStock);

        // 4. Crear el pedido y confirmar TODO (pedido nuevo + stock nuevo) en una sola transacción
        var order = new Order(command.CustomerName, command.ProductId, command.Quantity, unitPrice.Value,
            command.PickupDate);
        await orderRepository.AddAsync(order, cancellationToken);
        await unitOfWork.CompleteAsync(cancellationToken);

        return Result<Order>.Success(order);
    }
}
