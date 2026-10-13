using StockFlow.Contracts.Common;
using StockFlow.Contracts.Orders;

namespace StockFlow.Application.Orders;

public interface IOrderService
{
    Task<OrderDto> PlaceAsync(PlaceOrderRequest request, CancellationToken ct = default);
    Task<OrderDto> CancelAsync(int id, CancellationToken ct = default);
    Task<OrderDto> GetAsync(int id, CancellationToken ct = default);
    Task<PagedResult<OrderDto>> SearchAsync(string? status, int page, int pageSize, CancellationToken ct = default);
}