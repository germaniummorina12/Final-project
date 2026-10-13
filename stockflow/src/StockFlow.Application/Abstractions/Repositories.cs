using StockFlow.Application.Common;
using StockFlow.Application.Products;
using StockFlow.Domain.Entities;
using StockFlow.Domain.Enums;

namespace StockFlow.Application.Abstractions;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<Product>> GetByIdsAsync(IReadOnlyCollection<int> ids, CancellationToken ct = default);
    Task<bool> SkuExistsAsync(string sku, CancellationToken ct = default);
    Task<Page<Product>> SearchAsync(ProductSearchCriteria criteria, CancellationToken ct = default);
    Task<IReadOnlyList<Product>> GetLowStockAsync(CancellationToken ct = default);
    Task<Page<StockMovement>> GetMovementsAsync(int productId, int page, int pageSize, CancellationToken ct = default);
    void Add(Product product);
    void AddMovement(StockMovement movement);
}

public interface ICategoryRepository
{
    Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken ct = default);
    Task<Category?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<bool> NameExistsAsync(string name, CancellationToken ct = default);
    void Add(Category category);
}

public interface ICustomerRepository
{
    Task<IReadOnlyList<Customer>> GetAllAsync(CancellationToken ct = default);
    Task<Customer?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<bool> EmailExistsAsync(string email, CancellationToken ct = default);
    void Add(Customer customer);
}

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Page<Order>> SearchAsync(OrderStatus? status, int page, int pageSize, CancellationToken ct = default);
    void Add(Order order);
}