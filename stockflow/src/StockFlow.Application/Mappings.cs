using StockFlow.Contracts.Catalog;
using StockFlow.Contracts.Orders;
using StockFlow.Contracts.Products;
using StockFlow.Contracts.Realtime;
using StockFlow.Domain.Entities;

namespace StockFlow.Application;

internal static class Mappings
{
    public static CategoryDto ToDto(this Category c) => new(c.Id, c.Name);

    public static CustomerDto ToDto(this Customer c) => new(c.Id, c.Name, c.Email);

    public static ProductDto ToDto(this Product p) => new(
        p.Id, p.Sku, p.Name, p.Description, p.CategoryId, p.Category?.Name ?? string.Empty,
        p.UnitPrice, p.StockOnHand, p.ReorderLevel, p.IsLowStock, p.IsActive, p.ConcurrencyStamp);

    public static StockMovementDto ToDto(this StockMovement m) => new(
        m.Id, m.ProductId, m.Delta, m.Reason.ToString(), m.OccurredAtUtc, m.PerformedBy, m.Reference);

    public static OrderDto ToDto(this Order o) => new(
        o.Id, o.Number, o.CustomerId, o.Customer?.Name ?? string.Empty, o.Status.ToString(),
        o.PlacedAtUtc, o.CancelledAtUtc, o.PlacedBy, o.Total,
        o.Lines.Select(l => new OrderLineDto(l.ProductId, l.ProductSku, l.ProductName, l.UnitPrice, l.Quantity, l.LineTotal)).ToList());

    public static StockChangedMessage ToStockMessage(this Product p) => new(p.Id, p.Sku, p.StockOnHand, p.IsLowStock);
}