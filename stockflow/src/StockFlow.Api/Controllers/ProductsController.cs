using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using StockFlow.Application.Products;
using StockFlow.Api.Security;
using StockFlow.Contracts.Common;
using StockFlow.Contracts.Products;

namespace StockFlow.Api.Controllers;

[ApiController]
[Route("api/v1/products")]
public class ProductsController(IProductService products) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<ProductDto>>> Search([FromQuery] ProductSearchRequest request, CancellationToken ct) =>
        Ok(await products.SearchAsync(request, ct));

    [HttpGet("low-stock")]
    public async Task<ActionResult<IReadOnlyList<ProductDto>>> LowStock(CancellationToken ct) =>
        Ok(await products.GetLowStockAsync(ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProductDto>> Get(int id, CancellationToken ct) =>
        Ok(await products.GetAsync(id, ct));

    [HttpGet("{id:int}/movements")]
    public async Task<ActionResult<PagedResult<StockMovementDto>>> Movements(
        int id, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default) =>
        Ok(await products.GetMovementsAsync(id, Math.Max(page, 1), Math.Clamp(pageSize, 1, 100), ct));

    [HttpPost]
    [Authorize(Policy = Policies.ManageCatalog)]
    public async Task<ActionResult<ProductDto>> Create(CreateProductRequest request, CancellationToken ct)
    {
        var created = await products.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ProductDto>> Update(int id, UpdateProductRequest request, CancellationToken ct) =>
        Ok(await products.UpdateAsync(id, request, ct));

    [HttpPost("{id:int}/stock")]
    public async Task<ActionResult<ProductDto>> AdjustStock(int id, AdjustStockRequest request, CancellationToken ct) =>
        Ok(await products.AdjustStockAsync(id, request, ct));

    [HttpDelete("{id:int}")]
    [Authorize(Policy = Policies.AdminOnly)]
    public async Task<IActionResult> Deactivate(int id, CancellationToken ct)
    {
        await products.DeactivateAsync(id, ct);
        return NoContent();
    }
}