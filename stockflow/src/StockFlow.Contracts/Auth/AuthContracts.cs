using System.ComponentModel.DataAnnotations;

namespace StockFlow.Contracts.Auth;

public sealed class LoginRequest
{
    [Required, EmailAddress]
    public string Email { get; init; } = string.Empty;

    [Required]
    public string Password { get; init; } = string.Empty;
}

public sealed record LoginResponse(string Token, DateTime ExpiresAtUtc, string UserName, IReadOnlyList<string> Roles);