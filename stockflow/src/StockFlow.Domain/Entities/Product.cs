using StockFlow.Domain.Enums;
using StockFlow.Domain.Exceptions;

namespace StockFlow.Domain.Entities;

public class Product : Entity
{
    public string Sku { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public int CategoryId { get; private set; }
    public Category? Category { get; private set; }
    public decimal UnitPrice { get; private set; }
    public int StockOnHand { get; private set; }
    public int ReorderLevel { get; private set; }
    public bool IsActive { get; private set; } = true;
    public Guid ConcurrencyStamp { get; private set; } = Guid.NewGuid();

    public bool IsLowStock => StockOnHand <= ReorderLevel;

    private Product() { }

    public Product(string sku, string name, int categoryId, decimal unitPrice, int reorderLevel, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(sku)) throw new DomainException("SKU is required.");

        Sku = sku.Trim().ToUpperInvariant();
        UpdateDetails(name, description, categoryId, unitPrice, reorderLevel);
    }

    public void UpdateDetails(string name, string? description, int categoryId, decimal unitPrice, int reorderLevel)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("Product name is required.");
        if (categoryId <= 0) throw new DomainException("A category is required.");
        if (unitPrice <= 0) throw new DomainException("Unit price must be greater than zero.");
        if (reorderLevel < 0) throw new DomainException("Reorder level cannot be negative.");

        Name = name.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        CategoryId = categoryId;
        UnitPrice = decimal.Round(unitPrice, 2);
        ReorderLevel = reorderLevel;
        Touch();
    }

    public StockMovement Receive(int quantity, string performedBy, DateTime nowUtc, string? reference = null)
    {
        RequirePositive(quantity);
        return ApplyDelta(quantity, StockMovementReason.Received, performedBy, nowUtc, reference);
    }

    public StockMovement Sell(int quantity, string performedBy, DateTime nowUtc, string? reference = null)
    {
        RequirePositive(quantity);
        return ApplyDelta(-quantity, StockMovementReason.Sold, performedBy, nowUtc, reference);
    }

    public StockMovement Restock(int quantity, string performedBy, DateTime nowUtc, string? reference = null)
    {
        RequirePositive(quantity);
        return ApplyDelta(quantity, StockMovementReason.Returned, performedBy, nowUtc, reference);
    }

    public StockMovement Adjust(int delta, string performedBy, DateTime nowUtc, string? reference = null) =>
        ApplyDelta(delta, StockMovementReason.Adjustment, performedBy, nowUtc, reference);

    public void Deactivate()
    {
        IsActive = false;
        Touch();
    }

    private StockMovement ApplyDelta(int delta, StockMovementReason reason, string performedBy, DateTime nowUtc, string? reference)
    {
        if (delta == 0) throw new DomainException("The quantity change cannot be zero.");

        var newStock = StockOnHand + delta;
        if (newStock < 0) throw new InsufficientStockException(Sku, StockOnHand, -delta);

        StockOnHand = newStock;
        Touch();
        return new StockMovement(this, delta, reason, performedBy, nowUtc, reference);
    }

    private static void RequirePositive(int quantity)
    {
        if (quantity <= 0) throw new DomainException("Quantity must be greater than zero.");
    }

    private void Touch() => ConcurrencyStamp = Guid.NewGuid();
}