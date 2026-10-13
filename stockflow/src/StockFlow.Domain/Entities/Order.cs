using StockFlow.Domain.Enums;
using StockFlow.Domain.Exceptions;

namespace StockFlow.Domain.Entities;

public class Order : Entity
{
    private readonly List<OrderLine> _lines = [];

    public string Number { get; private set; } = string.Empty;
    public int CustomerId { get; private set; }
    public Customer? Customer { get; private set; }
    public OrderStatus Status { get; private set; }
    public DateTime PlacedAtUtc { get; private set; }
    public DateTime? CancelledAtUtc { get; private set; }
    public string PlacedBy { get; private set; } = string.Empty;
    public Guid ConcurrencyStamp { get; private set; } = Guid.NewGuid();

    public IReadOnlyCollection<OrderLine> Lines => _lines;
    public decimal Total => _lines.Sum(l => l.LineTotal);

    private Order() { }

    public static Order Place(Customer customer, string placedBy, DateTime nowUtc, IEnumerable<(Product Product, int Quantity)> items)
    {
        var order = new Order
        {
            Number = $"ORD-{nowUtc:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}",
            CustomerId = customer.Id,
            Customer = customer,
            PlacedBy = placedBy,
            PlacedAtUtc = nowUtc,
            Status = OrderStatus.Placed
        };

        foreach (var (product, quantity) in items)
        {
            order.AddLine(product, quantity);
        }

        if (order._lines.Count == 0) throw new DomainException("An order needs at least one line.");

        return order;
    }

    public void Cancel(DateTime nowUtc)
    {
        if (Status != OrderStatus.Placed) throw new DomainException("Only a placed order can be cancelled.");

        Status = OrderStatus.Cancelled;
        CancelledAtUtc = nowUtc;
        ConcurrencyStamp = Guid.NewGuid();
    }

    private void AddLine(Product product, int quantity)
    {
        if (quantity <= 0) throw new DomainException($"The quantity for {product.Sku} must be greater than zero.");
        if (!product.IsActive) throw new DomainException($"Product {product.Sku} is no longer available.");

        var existing = _lines.FirstOrDefault(l => l.ProductSku == product.Sku);
        if (existing is null)
        {
            _lines.Add(new OrderLine(product, quantity));
        }
        else
        {
            existing.IncreaseQuantity(quantity);
        }
    }
}