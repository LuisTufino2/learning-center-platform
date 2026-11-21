using System.Net.Mime;
using Acme.Bakery.Platform.Inventory.Application.CommandServices;
using Acme.Bakery.Platform.Inventory.Application.QueryServices;
using Acme.Bakery.Platform.Inventory.Domain.Model.Queries;
using Acme.Bakery.Platform.Inventory.Interfaces.Rest.Resources;
using Acme.Bakery.Platform.Inventory.Interfaces.Rest.Transform;
using Acme.Bakery.Platform.Shared.Interfaces.Rest.Transform;
using Microsoft.AspNetCore.Mvc;

namespace Acme.Bakery.Platform.Inventory.Interfaces.Rest;

// Controller REST de productos: /api/v1/products
[ApiController]
[Route("api/v1/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
[Tags("Products")]
public class ProductsController(
    IProductCommandService productCommandService,
    IProductQueryService productQueryService) : ControllerBase
{
    // POST /api/v1/products → registra un producto (201, o 400 / 409 si rompe una regla)
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

    // GET /api/v1/products/{productId} → devuelve un producto (200) o 404 si no existe
    [HttpGet("{productId:int}")]
    [ProducesResponseType(typeof(ProductResource), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProductById(int productId, CancellationToken cancellationToken)
    {
        var product = await productQueryService.Handle(new GetProductByIdQuery(productId), cancellationToken);
        if (product is null) return NotFound();
        return Ok(ProductResourceFromEntityAssembler.ToResourceFromEntity(product));
    }

    // GET /api/v1/products → lista todos los productos (200)
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ProductResource>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllProducts(CancellationToken cancellationToken)
    {
        var products = await productQueryService.Handle(new GetAllProductsQuery(), cancellationToken);
        return Ok(products.Select(ProductResourceFromEntityAssembler.ToResourceFromEntity));
    }
}
