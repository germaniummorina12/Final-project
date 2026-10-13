using StockFlow.Domain.Entities;
using StockFlow.Domain.Enums;
using StockFlow.Domain.Exceptions;

namespace StockFlow.Domain.Tests;

public class ProductTests
{
    private static readonly DateTime Now = new(2026, 9, 24, 10, 0, 0, DateTimeKind.Utc);

    private static Product NewProduct(int stock = 0, int reorderLevel = 5)
    {
        var product = new Product(" kb-001 ", "Keyboard", categoryId: 1, unitPrice: 50m, reorderLevel: reorderLevel);
        if (stock > 0)
        {
            product.Receive(stock, "seed", Now);
        }
        return product;
    }

    [Fact]
    public void The_sku_is_trimmed_and_upper_cased()
    {
        Assert.Equal("KB-001", NewProduct().Sku);
    }

    [Fact]
    public void Receive_increases_stock_and_returns_a_matching_ledger_entry()
    {
        var product = NewProduct();

        var movement = product.Receive(10, "ann", Now, "PO-77");

        Assert.Equal(10, product.StockOnHand);
        Assert.Equal(10, movement.Delta);
        Assert.Equal(StockMovementReason.Received, movement.Reason);
        Assert.Equal("ann", movement.PerformedBy);
        Assert.Equal("PO-77", movement.Reference);
    }

    [Fact]
    public void Sell_decreases_stock_and_records_a_negative_movement()
    {
        var product = NewProduct(stock: 10);

        var movement = product.Sell(4, "ann", Now);

        Assert.Equal(6, product.StockOnHand);
        Assert.Equal(-4, movement.Delta);
        Assert.Equal(StockMovementReason.Sold, movement.Reason);
    }

    [Fact]
    public void Selling_more_than_is_available_throws_and_leaves_stock_untouched()
    {
        var product = NewProduct(stock: 3);
        var stampBefore = product.ConcurrencyStamp;

        var ex = Assert.Throws<InsufficientStockException>(() => product.Sell(5, "ann", Now));

        Assert.Equal(3, product.StockOnHand);
        Assert.Equal(stampBefore, product.ConcurrencyStamp);
        Assert.Equal(3, ex.Available);
        Assert.Equal(5, ex.Requested);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-2)]
    public void Receive_and_sell_reject_non_positive_quantities(int quantity)
    {
        Assert.Throws<DomainException>(() => NewProduct(stock: 5).Receive(quantity, "ann", Now));
        Assert.Throws<DomainException>(() => NewProduct(stock: 5).Sell(quantity, "ann", Now));
    }

    [Fact]
    public void An_adjustment_cannot_take_stock_below_zero()
    {
        var product = NewProduct(stock: 2);

        Assert.Throws<InsufficientStockException>(() => product.Adjust(-3, "ann", Now));
        Assert.Equal(2, product.StockOnHand);
    }

    [Fact]
    public void IsLowStock_is_true_at_or_below_the_reorder_level()
    {
        var product = NewProduct(stock: 5, reorderLevel: 5);
        Assert.True(product.IsLowStock);

        product.Receive(1, "ann", Now);
        Assert.False(product.IsLowStock);
    }

    [Fact]
    public void Every_change_produces_a_new_concurrency_stamp()
    {
        var product = NewProduct();
        var before = product.ConcurrencyStamp;

        product.Receive(1, "ann", Now);

        Assert.NotEqual(before, product.ConcurrencyStamp);
    }

    [Fact]
    public void The_price_must_be_positive()
    {
        Assert.Throws<DomainException>(() => new Product("X1", "Thing", 1, 0m, 0));
    }
}