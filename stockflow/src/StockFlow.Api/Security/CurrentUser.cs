using StockFlow.Application.Abstractions;

namespace StockFlow.Api.Security;

public sealed class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    public string UserName => accessor.HttpContext?.User.Identity?.Name ?? "anonymous";
}