using System.ComponentModel.DataAnnotations;

namespace StockFlow.Contracts.Products;

public sealed record ProductDto(
    int Id,
    string Sku,
    string Name,
    string? Description,
    int CategoryId,
    string CategoryName,
    decimal UnitPrice,
    int StockOnHand,
    int ReorderLevel,
    bool IsLowStock,
    bool IsActive,
    Guid ConcurrencyStamp);

public sealed class CreateProductRequest
{
    [Required, StringLength(40, MinimumLength = 2)]
    public string Sku { get; init; } = string.Empty;

    [Required, StringLength(200)]
    public string Name { get; init; } = string.Empty;

    [StringLength(2000)]
    public string? Description { get; init; }

    [Range(1, int.MaxValue)]
    public int CategoryId { get; init; }

    [Range(0.01, 1_000_000)]
    public decimal UnitPrice { get; init; }

    [Range(0, 1_000_000)]
    public int InitialStock { get; init; }

    [Range(0, 1_000_000)]
    public int ReorderLevel { get; init; }
}

public sealed class UpdateProductRequest
{
    [Required, StringLength(200)]
    public string Name { get; init; } = string.Empty;

    [StringLength(2000)]
    public string? Description { get; init; }

    [Range(1, int.MaxValue)]
    public int CategoryId { get; init; }

    [Range(0.01, 1_000_000)]
    public decimal UnitPrice { get; init; }

    [Range(0, 1_000_000)]
    public int ReorderLevel { get; init; }
    public Guid ConcurrencyStamp { get; init; }
}

public sealed class AdjustStockRequest
{
    [Range(-1_000_000, 1_000_000)]
    public int Delta { get; init; }

    [Required, AllowedValues("Received", "Adjustment")]
    public string Reason { get; init; } = string.Empty;

    [StringLength(200)]
    public string? Note { get; init; }
}

public sealed class ProductSearchRequest
{
    public string? Search { get; init; }
    public int? CategoryId { get; init; }
    public bool LowStockOnly { get; init; }
    public bool IncludeInactive { get; init; }
    public string? Sort { get; init; }
    public bool Descending { get; init; }

    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    [Range(1, 100)]
    public int PageSize { get; init; } = 20;
}

public sealed record StockMovementDto(
    int Id,
    int ProductId,
    int Delta,
    string Reason,
    DateTime OccurredAtUtc,
    string PerformedBy,
    string? Reference);