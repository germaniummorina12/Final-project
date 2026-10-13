using Microsoft.EntityFrameworkCore;
using StockFlow.Application.Abstractions;
using StockFlow.Domain.Entities;

namespace StockFlow.Infrastructure.Persistence.Repositories;

internal sealed class CategoryRepository(AppDbContext db) : ICategoryRepository
{
    public async Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken ct = default) =>
        await db.Categories.AsNoTracking().OrderBy(c => c.Name).ToListAsync(ct);

    public Task<Category?> GetByIdAsync(int id, CancellationToken ct = default) =>
        db.Categories.FirstOrDefaultAsync(c => c.Id == id, ct);

    public Task<bool> NameExistsAsync(string name, CancellationToken ct = default) =>
        db.Categories.AnyAsync(c => c.Name == name, ct);

    public void Add(Category category) => db.Categories.Add(category);
}

internal sealed class CustomerRepository(AppDbContext db) : ICustomerRepository
{
    public async Task<IReadOnlyList<Customer>> GetAllAsync(CancellationToken ct = default) =>
        await db.Customers.AsNoTracking().OrderBy(c => c.Name).ToListAsync(ct);

    public Task<Customer?> GetByIdAsync(int id, CancellationToken ct = default) =>
        db.Customers.FirstOrDefaultAsync(c => c.Id == id, ct);

    public Task<bool> EmailExistsAsync(string email, CancellationToken ct = default) =>
        db.Customers.AnyAsync(c => c.Email == email, ct);

    public void Add(Customer customer) => db.Customers.Add(customer);
}