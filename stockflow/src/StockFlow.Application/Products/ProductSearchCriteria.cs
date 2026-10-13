namespace StockFlow.Application.Products;

public enum ProductSort { Name, Sku, Stock }

public sealed record ProductSearchCriteria(
    string? Search,
    int? CategoryId,
    bool LowStockOnly,
    bool IncludeInactive,
    ProductSort Sort,
    bool Descending,
    int Page,
    int PageSize);