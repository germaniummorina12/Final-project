namespace StockFlow.Domain.Exceptions;

public sealed class InsufficientStockException(string sku, int available, int requested)
    : DomainException($"Insufficient stock for {sku}: {available} available, {requested} requested.")
{
    public string Sku { get; } = sku;
    public int Available { get; } = available;
    public int Requested { get; } = requested;
}