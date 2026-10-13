using StockFlow.Application.Abstractions;
using StockFlow.Application.Exceptions;
using StockFlow.Contracts.Common;
using StockFlow.Contracts.Orders;
using StockFlow.Domain.Entities;
using StockFlow.Domain.Enums;

namespace StockFlow.Application.Orders;

internal sealed class OrderService(
    IOrderRepository orders,
    ICustomerRepository customers,
    IProductRepository products,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IStockNotifier notifier,
    TimeProvider clock) : IOrderService
{
    public async Task<OrderDto> PlaceAsync(PlaceOrderRequest request, CancellationToken ct = default)
    {
        var customer = await customers.GetByIdAsync(request.CustomerId, ct)
                       ?? throw new NotFoundException(nameof(Customer), request.CustomerId);
        var wanted = request.Lines
            .GroupBy(l => l.ProductId)
            .ToDictionary(g => g.Key, g => g.Sum(l => l.Quantity));

        var found = await products.GetByIdsAsync(wanted.Keys, ct);
        var missing = wanted.Keys.Except(found.Select(p => p.Id)).ToList();
        if (missing.Count > 0)
        {
            throw new NotFoundException(nameof(Product), string.Join(", ", missing));
        }

        var now = clock.GetUtcNow().UtcDateTime;
        var user = currentUser.UserName;
        var order = Order.Place(customer, user, now, found.Select(p => (p, wanted[p.Id])));
        foreach (var line in order.Lines)
        {
            var product = found.Single(p => p.Id == line.ProductId);
            products.AddMovement(product.Sell(line.Quantity, user, now, order.Number));
        }
        orders.Add(order);
        await unitOfWork.SaveChangesAsync(ct);
        foreach (var product in found)
        {
            await notifier.NotifyAsync(product.ToStockMessage(), ct);
        }

        return order.ToDto();
    }

    public async Task<OrderDto> CancelAsync(int id, CancellationToken ct = default)
    {
        var order = await orders.GetByIdAsync(id, ct) ?? throw new NotFoundException(nameof(Order), id);

        if (order.Status == OrderStatus.Cancelled)
        {
            throw new ConflictException($"Order {order.Number} is already cancelled.");
        }

        var now = clock.GetUtcNow().UtcDateTime;
        var user = currentUser.UserName;
        var found = await products.GetByIdsAsync(order.Lines.Select(l => l.ProductId).ToList(), ct);

        order.Cancel(now);
        foreach (var line in order.Lines)
        {
            var product = found.Single(p => p.Id == line.ProductId);
            products.AddMovement(product.Restock(line.Quantity, user, now, order.Number));
        }

        await unitOfWork.SaveChangesAsync(ct);

        foreach (var product in found)
        {
            await notifier.NotifyAsync(product.ToStockMessage(), ct);
        }

        return order.ToDto();
    }

    public async Task<OrderDto> GetAsync(int id, CancellationToken ct = default)
    {
        var order = await orders.GetByIdAsync(id, ct) ?? throw new NotFoundException(nameof(Order), id);
        return order.ToDto();
    }

    public async Task<PagedResult<OrderDto>> SearchAsync(string? status, int page, int pageSize, CancellationToken ct = default)
    {
        OrderStatus? parsed = Enum.TryParse<OrderStatus>(status, ignoreCase: true, out var value) ? value : null;

        var result = await orders.SearchAsync(parsed, page, pageSize, ct);
        return new PagedResult<OrderDto>(result.Items.Select(o => o.ToDto()).ToList(), page, pageSize, result.TotalCount);
    }
}