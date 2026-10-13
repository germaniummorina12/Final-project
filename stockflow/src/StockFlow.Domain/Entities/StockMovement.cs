using StockFlow.Domain.Enums;

namespace StockFlow.Domain.Entities;

public class StockMovement : Entity
{
    public int ProductId { get; private set; }
    public Product? Product { get; private set; }
    public int Delta { get; private set; }
    public StockMovementReason Reason { get; private set; }
    public DateTime OccurredAtUtc { get; private set; }
    public string PerformedBy { get; private set; } = string.Empty;
    public string? Reference { get; private set; }

    private StockMovement() { }

    internal StockMovement(Product product, int delta, StockMovementReason reason, string performedBy, DateTime occurredAtUtc, string? reference)
    {
        Product = product;
        ProductId = product.Id;
        Delta = delta;
        Reason = reason;
        PerformedBy = performedBy;
        OccurredAtUtc = occurredAtUtc;
        Reference = reference;
    }
}