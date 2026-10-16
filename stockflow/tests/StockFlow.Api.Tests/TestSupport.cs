using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using StockFlow.Contracts.Auth;
using StockFlow.Contracts.Orders;
using StockFlow.Contracts.Products;

namespace StockFlow.Api.Tests;

internal static class Users
{
    public const string Admin = "admin@stockflow.local";
    public const string Manager = "manager@stockflow.local";
    public const string Clerk = "clerk@stockflow.local";
    public const string Password = "StockFlow#2026";
}

internal static class TestData
{
    public static string UniqueSku() => $"T-{Guid.NewGuid():N}"[..14];

    public static CreateProductRequest NewProduct(string? sku = null, int initialStock = 10, int reorderLevel = 2) => new()
    {
        Sku = sku ?? UniqueSku(),
        Name = "Test product",
        CategoryId = 1,
        UnitPrice = 19.99m,
        InitialStock = initialStock,
        ReorderLevel = reorderLevel
    };

    public static PlaceOrderRequest Order(params (int ProductId, int Quantity)[] lines) => new()
    {
        CustomerId = 1,
        Lines = lines.Select(l => new PlaceOrderLineRequest { ProductId = l.ProductId, Quantity = l.Quantity }).ToList()
    };
}

internal static class ApiClientExtensions
{
    public static async Task<HttpClient> CreateClientAsync(this ApiFactory factory, string email)
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest { Email = email, Password = Users.Password });
        response.EnsureSuccessStatusCode();

        var login = await response.Content.ReadFromJsonAsync<LoginResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login!.Token);
        return client;
    }

    public static async Task<ProductDto> CreateProductAsync(this HttpClient client, CreateProductRequest request)
    {
        var response = await client.PostAsJsonAsync("/api/v1/products", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<ProductDto>())!;
    }

    public static async Task<ProductDto> GetProductAsync(this HttpClient client, int id) =>
        (await client.GetFromJsonAsync<ProductDto>($"/api/v1/products/{id}"))!;
}