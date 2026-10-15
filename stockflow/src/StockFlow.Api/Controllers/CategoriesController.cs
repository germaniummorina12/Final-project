using Microsoft.AspNetCore.Mvc;
using StockFlow.Application.Catalog;
using StockFlow.Contracts.Catalog;

namespace StockFlow.Api.Controllers;

[ApiController]
[Route("api/v1/categories")]
public class CategoriesController(ICategoryService categories) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CategoryDto>>> GetAll(CancellationToken ct) =>
        Ok(await categories.GetAllAsync(ct));

    [HttpPost]
    public async Task<ActionResult<CategoryDto>> Create(CreateCategoryRequest request, CancellationToken ct)
    {
        var created = await categories.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetAll), created);
    }
}