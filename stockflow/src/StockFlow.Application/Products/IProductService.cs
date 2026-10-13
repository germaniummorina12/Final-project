using StockFlow.Contracts.Common;
using StockFlow.Contracts.Products;

namespace StockFlow.Application.Products;

public interface IProductService
{
    Task<PagedResult<ProductDto>> SearchAsync(ProductSearchRequest request, CancellationToken ct = default);
    Task<ProductDto> GetAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<ProductDto>> GetLowStockAsync(CancellationToken ct = default);
    Task<PagedResult<StockMovementDto>> GetMovementsAsync(int id, int page, int pageSize, CancellationToken ct = default);
    Task<ProductDto> CreateAsync(CreateProductRequest request, CancellationToken ct = default);
    Task<ProductDto> UpdateAsync(int id, UpdateProductRequest request, CancellationToken ct = default);
    Task<ProductDto> AdjustStockAsync(int id, AdjustStockRequest request, CancellationToken ct = default);
    Task DeactivateAsync(int id, CancellationToken ct = default);
}