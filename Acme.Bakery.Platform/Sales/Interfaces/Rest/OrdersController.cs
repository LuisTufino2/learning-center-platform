using System.Net.Mime;
using Acme.Bakery.Platform.Sales.Application.CommandServices;
using Acme.Bakery.Platform.Sales.Application.QueryServices;
using Acme.Bakery.Platform.Sales.Domain.Model.Queries;
using Acme.Bakery.Platform.Sales.Interfaces.Rest.Resources;
using Acme.Bakery.Platform.Sales.Interfaces.Rest.Transform;
using Acme.Bakery.Platform.Shared.Interfaces.Rest.Transform;
using Microsoft.AspNetCore.Mvc;

namespace Acme.Bakery.Platform.Sales.Interfaces.Rest;

// Controller REST de pedidos: /api/v1/orders
[ApiController]
[Route("api/v1/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
[Tags("Orders")]
public class OrdersController(
    IOrderCommandService orderCommandService,
    IOrderQueryService orderQueryService) : ControllerBase
{
    // POST /api/v1/orders → registra un pedido y descuenta stock (201, o 400 / 404 / 409)
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

    // GET /api/v1/orders/{orderId} → devuelve un pedido (200) o 404 si no existe
    [HttpGet("{orderId:int}")]
    [ProducesResponseType(typeof(OrderResource), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrderById(int orderId, CancellationToken cancellationToken)
    {
        var order = await orderQueryService.Handle(new GetOrderByIdQuery(orderId), cancellationToken);
        if (order is null) return NotFound();
        return Ok(OrderResourceFromEntityAssembler.ToResourceFromEntity(order));
    }

    // GET /api/v1/orders → lista todos los pedidos (200)
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<OrderResource>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllOrders(CancellationToken cancellationToken)
    {
        var orders = await orderQueryService.Handle(new GetAllOrdersQuery(), cancellationToken);
        return Ok(orders.Select(OrderResourceFromEntityAssembler.ToResourceFromEntity));
    }
}
