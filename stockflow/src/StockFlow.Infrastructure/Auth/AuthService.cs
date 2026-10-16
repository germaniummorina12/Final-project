using Microsoft.AspNetCore.Identity;
using StockFlow.Application.Abstractions;
using StockFlow.Application.Exceptions;
using StockFlow.Contracts.Auth;

namespace StockFlow.Infrastructure.Auth;

internal sealed class AuthService(UserManager<AppUser> users, IJwtTokenService tokens) : IAuthService
{
    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var user = await users.FindByEmailAsync(request.Email);

        if (user is null || !await users.CheckPasswordAsync(user, request.Password))
        {
            throw new UnauthorizedException("Invalid email or password.");
        }

        var roles = await users.GetRolesAsync(user);
        var (token, expiresAtUtc) = tokens.CreateToken(user, roles);

        return new LoginResponse(token, expiresAtUtc, user.UserName ?? user.Email ?? string.Empty, roles.ToList());
    }
}