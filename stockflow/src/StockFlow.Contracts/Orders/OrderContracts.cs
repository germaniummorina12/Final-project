using System.ComponentModel.DataAnnotations;

namespace StockFlow.Contracts.Orders;

public sealed class PlaceOrderRequest
{
    [Range(1, int.MaxValue)]
    public int CustomerId { get; init; }

    [MinLength(1)]
    public List<PlaceOrderLineRequest> Lines { get; init; } = [];
}

public sealed class PlaceOrderLineRequest
{
    [Range(1, int.MaxValue)]
    public int ProductId { get; init; }

    [Range(1, 10_000)]
    public int Quantity { get; init; }
}

public sealed record OrderLineDto(int ProductId, string ProductSku, string ProductName, decimal UnitPrice, int Quantity, decimal LineTotal);

public sealed record OrderDto(
    int Id,
    string Number,
    int CustomerId,
    string CustomerName,
    string Status,
    DateTime PlacedAtUtc,
    DateTime? CancelledAtUtc,
    string PlacedBy,
    decimal Total,
    IReadOnlyList<OrderLineDto> Lines);