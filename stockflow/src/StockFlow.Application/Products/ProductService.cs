using StockFlow.Application.Abstractions;
using StockFlow.Application.Exceptions;
using StockFlow.Contracts.Common;
using StockFlow.Contracts.Products;
using StockFlow.Domain.Entities;

namespace StockFlow.Application.Products;

internal sealed class ProductService(
    IProductRepository products,
    ICategoryRepository categories,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IStockNotifier notifier,
    TimeProvider clock) : IProductService
{
    public async Task<PagedResult<ProductDto>> SearchAsync(ProductSearchRequest request, CancellationToken ct = default)
    {
        var criteria = new ProductSearchCriteria(
            request.Search,
            request.CategoryId,
            request.LowStockOnly,
            request.IncludeInactive,
            ParseSort(request.Sort),
            request.Descending,
            request.Page,
            request.PageSize);

        var page = await products.SearchAsync(criteria, ct);
        var items = page.Items.Select(p => p.ToDto()).ToList();
        return new PagedResult<ProductDto>(items, request.Page, request.PageSize, page.TotalCount);
    }

    public async Task<ProductDto> GetAsync(int id, CancellationToken ct = default)
    {
        var product = await products.GetByIdAsync(id, ct) ?? throw new NotFoundException(nameof(Product), id);
        return product.ToDto();
    }

    public async Task<IReadOnlyList<ProductDto>> GetLowStockAsync(CancellationToken ct = default) =>
        (await products.GetLowStockAsync(ct)).Select(p => p.ToDto()).ToList();

    public async Task<PagedResult<StockMovementDto>> GetMovementsAsync(int id, int page, int pageSize, CancellationToken ct = default)
    {
        _ = await products.GetByIdAsync(id, ct) ?? throw new NotFoundException(nameof(Product), id);

        var result = await products.GetMovementsAsync(id, page, pageSize, ct);
        return new PagedResult<StockMovementDto>(result.Items.Select(m => m.ToDto()).ToList(), page, pageSize, result.TotalCount);
    }

    public async Task<ProductDto> CreateAsync(CreateProductRequest request, CancellationToken ct = default)
    {
        _ = await categories.GetByIdAsync(request.CategoryId, ct) ?? throw new NotFoundException(nameof(Category), request.CategoryId);

        if (await products.SkuExistsAsync(request.Sku, ct))
        {
            throw new ConflictException($"A product with SKU '{request.Sku.Trim().ToUpperInvariant()}' already exists.");
        }

        var now = clock.GetUtcNow().UtcDateTime;
        var product = new Product(request.Sku, request.Name, request.CategoryId, request.UnitPrice, request.ReorderLevel, request.Description);
        products.Add(product);

        if (request.InitialStock > 0)
        {
            products.AddMovement(product.Receive(request.InitialStock, currentUser.UserName, now, "Initial stock"));
        }

        await unitOfWork.SaveChangesAsync(ct);
        await notifier.NotifyAsync(product.ToStockMessage(), ct);

        return (await products.GetByIdAsync(product.Id, ct))!.ToDto();
    }

    public async Task<ProductDto> UpdateAsync(int id, UpdateProductRequest request, CancellationToken ct = default)
    {
        var product = await products.GetByIdAsync(id, ct) ?? throw new NotFoundException(nameof(Product), id);

        if (product.ConcurrencyStamp != request.ConcurrencyStamp)
        {
            throw new ConcurrencyConflictException("The product was changed by someone else. Reload it and try again.");
        }

        _ = await categories.GetByIdAsync(request.CategoryId, ct) ?? throw new NotFoundException(nameof(Category), request.CategoryId);

        product.UpdateDetails(request.Name, request.Description, request.CategoryId, request.UnitPrice, request.ReorderLevel);
        await unitOfWork.SaveChangesAsync(ct);

        return (await products.GetByIdAsync(id, ct))!.ToDto();
    }

    public async Task<ProductDto> AdjustStockAsync(int id, AdjustStockRequest request, CancellationToken ct = default)
    {
        var product = await products.GetByIdAsync(id, ct) ?? throw new NotFoundException(nameof(Product), id);
        var now = clock.GetUtcNow().UtcDateTime;

        var movement = request.Reason == "Received"
            ? product.Receive(request.Delta, currentUser.UserName, now, request.Note)
            : product.Adjust(request.Delta, currentUser.UserName, now, request.Note);

        products.AddMovement(movement);
        await unitOfWork.SaveChangesAsync(ct);
        await notifier.NotifyAsync(product.ToStockMessage(), ct);

        return product.ToDto();
    }

    public async Task DeactivateAsync(int id, CancellationToken ct = default)
    {
        var product = await products.GetByIdAsync(id, ct) ?? throw new NotFoundException(nameof(Product), id);

        product.Deactivate();
        await unitOfWork.SaveChangesAsync(ct);
    }

    private static ProductSort ParseSort(string? sort) => sort?.ToLowerInvariant() switch
    {
        "sku" => ProductSort.Sku,
        "stock" => ProductSort.Stock,
        _ => ProductSort.Name
    };
}