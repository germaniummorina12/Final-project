using System.Net;
using System.Net.Http.Json;
using StockFlow.Contracts.Auth;

namespace StockFlow.Api.Tests;

public sealed class SecurityTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task Requests_without_a_token_are_rejected()
    {
        var response = await factory.CreateClient().GetAsync("/api/v1/products");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Health_is_open_to_anyone()
    {
        var response = await factory.CreateClient().GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Login_with_a_wrong_password_returns_401()
    {
        var response = await factory.CreateClient().PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest { Email = Users.Manager, Password = "not-the-password" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task A_clerk_cannot_create_products()
    {
        var clerk = await factory.CreateClientAsync(Users.Clerk);

        var response = await clerk.PostAsJsonAsync("/api/v1/products", TestData.NewProduct());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Only_an_admin_can_deactivate_a_product()
    {
        var manager = await factory.CreateClientAsync(Users.Manager);
        var admin = await factory.CreateClientAsync(Users.Admin);
        var product = await manager.CreateProductAsync(TestData.NewProduct());

        var asManager = await manager.DeleteAsync($"/api/v1/products/{product.Id}");
        var asAdmin = await admin.DeleteAsync($"/api/v1/products/{product.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, asManager.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, asAdmin.StatusCode);
    }
}