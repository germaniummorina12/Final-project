using Microsoft.EntityFrameworkCore;
using StockFlow.Application.Abstractions;
using StockFlow.Application.Common;
using StockFlow.Application.Products;
using StockFlow.Domain.Entities;

namespace StockFlow.Infrastructure.Persistence.Repositories;

internal sealed class ProductRepository(AppDbContext db) : IProductRepository
{
    public Task<Product?> GetByIdAsync(int id, CancellationToken ct = default) =>
        db.Products.Include(p => p.Category).FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<IReadOnlyList<Product>> GetByIdsAsync(IReadOnlyCollection<int> ids, CancellationToken ct = default) =>
        await db.Products.Include(p => p.Category).Where(p => ids.Contains(p.Id)).ToListAsync(ct);

    public Task<bool> SkuExistsAsync(string sku, CancellationToken ct = default)
    {
        var normalized = sku.Trim().ToUpperInvariant();
        return db.Products.AnyAsync(p => p.Sku == normalized, ct);
    }

    public async Task<Page<Product>> SearchAsync(ProductSearchCriteria criteria, CancellationToken ct = default)
    {
        IQueryable<Product> query = db.Products.AsNoTracking().Include(p => p.Category);

        if (!string.IsNullOrWhiteSpace(criteria.Search))
        {
            var term = $"%{criteria.Search.Trim()}%";
            query = query.Where(p => EF.Functions.Like(p.Name, term) || EF.Functions.Like(p.Sku, term));
        }

        if (criteria.CategoryId is { } categoryId)
        {
            query = query.Where(p => p.CategoryId == categoryId);
        }

        if (criteria.LowStockOnly)
        {
            query = query.Where(p => p.StockOnHand <= p.ReorderLevel);
        }

        if (!criteria.IncludeInactive)
        {
            query = query.Where(p => p.IsActive);
        }

        query = (criteria.Sort, criteria.Descending) switch
        {
            (ProductSort.Sku, false) => query.OrderBy(p => p.Sku),
            (ProductSort.Sku, true) => query.OrderByDescending(p => p.Sku),
            (ProductSort.Stock, false) => query.OrderBy(p => p.StockOnHand).ThenBy(p => p.Id),
            (ProductSort.Stock, true) => query.OrderByDescending(p => p.StockOnHand).ThenBy(p => p.Id),
            (_, true) => query.OrderByDescending(p => p.Name).ThenBy(p => p.Id),
            _ => query.OrderBy(p => p.Name).ThenBy(p => p.Id)
        };

        var total = await query.CountAsync(ct);
        var items = await query
            .Skip((criteria.Page - 1) * criteria.PageSize)
            .Take(criteria.PageSize)
            .ToListAsync(ct);

        return new Page<Product>(items, total);
    }

    public async Task<IReadOnlyList<Product>> GetLowStockAsync(CancellationToken ct = default) =>
        await db.Products.AsNoTracking().Include(p => p.Category)
            .Where(p => p.IsActive && p.StockOnHand <= p.ReorderLevel)
            .OrderBy(p => p.StockOnHand).ThenBy(p => p.Name)
            .ToListAsync(ct);

    public async Task<Page<StockMovement>> GetMovementsAsync(int productId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = db.StockMovements.AsNoTracking().Where(m => m.ProductId == productId);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(m => m.OccurredAtUtc).ThenByDescending(m => m.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new Page<StockMovement>(items, total);
    }

    public void Add(Product product) => db.Products.Add(product);

    public void AddMovement(StockMovement movement) => db.StockMovements.Add(movement);
}