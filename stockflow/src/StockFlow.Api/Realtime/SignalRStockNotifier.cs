using Microsoft.AspNetCore.SignalR;
using StockFlow.Api.Hubs;
using StockFlow.Application.Abstractions;
using StockFlow.Contracts.Realtime;

namespace StockFlow.Api.Realtime;

public sealed class SignalRStockNotifier(IHubContext<StockHub> hub) : IStockNotifier
{
    public Task NotifyAsync(StockChangedMessage message, CancellationToken ct = default) =>
        hub.Clients.All.SendAsync("stockChanged", message, ct);
}