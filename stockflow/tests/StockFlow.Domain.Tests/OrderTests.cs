using StockFlow.Domain.Entities;
using StockFlow.Domain.Enums;
using StockFlow.Domain.Exceptions;

namespace StockFlow.Domain.Tests;

public class OrderTests
{
    private static readonly DateTime Now = new(2026, 9, 24, 10, 0, 0, DateTimeKind.Utc);

    private static Product NewProduct(string sku, decimal price)
    {
        var product = new Product(sku, sku + " item", 1, price, 0);
        product.Receive(100, "seed", Now);
        return product;
    }

    private static Customer NewCustomer() => new("Ann Lee", "ann@example.com");

    [Fact]
    public void Placing_an_order_creates_lines_a_total_and_a_number()
    {
        var order = Order.Place(NewCustomer(), "clerk", Now, [(NewProduct("A", 10m), 2), (NewProduct("B", 5.5m), 1)]);

        Assert.Equal(OrderStatus.Placed, order.Status);
        Assert.Equal(2, order.Lines.Count);
        Assert.Equal(25.5m, order.Total);
        Assert.StartsWith("ORD-20260924-", order.Number);
    }

    [Fact]
    public void An_order_needs_at_least_one_line()
    {
        Assert.Throws<DomainException>(() => Order.Place(NewCustomer(), "clerk", Now, []));
    }

    [Fact]
    public void The_same_product_twice_is_merged_into_one_line()
    {
        var product = NewProduct("A", 10m);

        var order = Order.Place(NewCustomer(), "clerk", Now, [(product, 2), (product, 3)]);

        var line = Assert.Single(order.Lines);
        Assert.Equal(5, line.Quantity);
    }

    [Fact]
    public void An_inactive_product_cannot_be_ordered()
    {
        var product = NewProduct("A", 10m);
        product.Deactivate();

        Assert.Throws<DomainException>(() => Order.Place(NewCustomer(), "clerk", Now, [(product, 1)]));
    }

    [Fact]
    public void A_line_keeps_the_price_from_the_time_of_sale()
    {
        var product = NewProduct("A", 10m);
        var order = Order.Place(NewCustomer(), "clerk", Now, [(product, 1)]);

        product.UpdateDetails(product.Name, null, 1, 99m, 0);

        Assert.Equal(10m, order.Lines.Single().UnitPrice);
    }

    [Fact]
    public void A_placed_order_can_be_cancelled_once()
    {
        var order = Order.Place(NewCustomer(), "clerk", Now, [(NewProduct("A", 10m), 1)]);

        order.Cancel(Now.AddHours(1));

        Assert.Equal(OrderStatus.Cancelled, order.Status);
        Assert.Equal(Now.AddHours(1), order.CancelledAtUtc);
        Assert.Throws<DomainException>(() => order.Cancel(Now.AddHours(2)));
    }
}