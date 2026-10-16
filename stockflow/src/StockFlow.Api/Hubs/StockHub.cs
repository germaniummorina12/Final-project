using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace StockFlow.Api.Hubs;

[Authorize]
public class StockHub : Hub
{
    // The server only pushes to clients here; clients send no messages.
}