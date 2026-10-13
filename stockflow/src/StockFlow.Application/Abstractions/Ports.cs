using StockFlow.Contracts.Auth;
using StockFlow.Contracts.Realtime;

namespace StockFlow.Application.Abstractions;

public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken ct = default);
}

public interface ICurrentUser
{
    string UserName { get; }
}

public interface IStockNotifier
{
    Task NotifyAsync(StockChangedMessage message, CancellationToken ct = default);
}

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct = default);
}