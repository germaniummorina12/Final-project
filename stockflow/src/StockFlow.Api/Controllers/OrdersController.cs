using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using StockFlow.Application.Orders;
using StockFlow.Api.Security;
using StockFlow.Contracts.Common;
using StockFlow.Contracts.Orders;

namespace StockFlow.Api.Controllers;

[ApiController]
[Route("api/v1/orders")]
public class OrdersController(IOrderService orders) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<OrderDto>>> Search(
        [FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default) =>
        Ok(await orders.SearchAsync(status, Math.Max(page, 1), Math.Clamp(pageSize, 1, 100), ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<OrderDto>> Get(int id, CancellationToken ct) =>
        Ok(await orders.GetAsync(id, ct));

    [HttpPost]
    public async Task<ActionResult<OrderDto>> Place(PlaceOrderRequest request, CancellationToken ct)
    {
        var placed = await orders.PlaceAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = placed.Id }, placed);
    }

    [HttpPost("{id:int}/cancel")]
    [Authorize(Policy = Policies.ManageCatalog)]
    public async Task<ActionResult<OrderDto>> Cancel(int id, CancellationToken ct) =>
        Ok(await orders.CancelAsync(id, ct));
}