using StockFlow.Application.Abstractions;
using StockFlow.Contracts.Realtime;

namespace StockFlow.Api.Realtime;

public sealed class NullStockNotifier : IStockNotifier
{
    public Task NotifyAsync(StockChangedMessage message, CancellationToken ct = default) => Task.CompletedTask;
}