using Microsoft.AspNetCore.Mvc;
using StockFlow.Application.Catalog;
using StockFlow.Contracts.Catalog;

namespace StockFlow.Api.Controllers;

[ApiController]
[Route("api/v1/customers")]
public class CustomersController(ICustomerService customers) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CustomerDto>>> GetAll(CancellationToken ct) =>
        Ok(await customers.GetAllAsync(ct));

    [HttpPost]
    public async Task<ActionResult<CustomerDto>> Create(CreateCustomerRequest request, CancellationToken ct)
    {
        var created = await customers.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetAll), created);
    }
}