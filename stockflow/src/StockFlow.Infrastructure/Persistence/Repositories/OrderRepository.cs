using Microsoft.EntityFrameworkCore;
using StockFlow.Application.Abstractions;
using StockFlow.Application.Common;
using StockFlow.Domain.Entities;
using StockFlow.Domain.Enums;

namespace StockFlow.Infrastructure.Persistence.Repositories;

internal sealed class OrderRepository(AppDbContext db) : IOrderRepository
{
    public Task<Order?> GetByIdAsync(int id, CancellationToken ct = default) =>
        db.Orders.Include(o => o.Customer).Include(o => o.Lines).FirstOrDefaultAsync(o => o.Id == id, ct);

    public async Task<Page<Order>> SearchAsync(OrderStatus? status, int page, int pageSize, CancellationToken ct = default)
    {
        IQueryable<Order> query = db.Orders.AsNoTracking().Include(o => o.Customer).Include(o => o.Lines);

        if (status is { } wanted)
        {
            query = query.Where(o => o.Status == wanted);
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(o => o.PlacedAtUtc).ThenByDescending(o => o.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new Page<Order>(items, total);
    }

    public void Add(Order order) => db.Orders.Add(order);
}