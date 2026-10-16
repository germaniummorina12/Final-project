using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockFlow.Application.Abstractions;
using StockFlow.Contracts.Auth;

namespace StockFlow.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController(IAuthService auth) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request, CancellationToken ct) =>
        Ok(await auth.LoginAsync(request, ct));

    [HttpGet("me")]
    public ActionResult<object> Me() => Ok(new
    {
        userName = User.Identity?.Name,
        roles = User.FindAll("role").Select(c => c.Value).ToArray()
    });
}