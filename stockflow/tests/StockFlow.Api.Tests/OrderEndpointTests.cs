using System.Net;
using System.Net.Http.Json;
using StockFlow.Contracts.Common;
using StockFlow.Contracts.Orders;
using StockFlow.Contracts.Products;

namespace StockFlow.Api.Tests;

public sealed class OrderEndpointTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task Placing_an_order_reduces_stock_and_writes_a_ledger_entry()
    {
        var manager = await factory.CreateClientAsync(Users.Manager);
        var clerk = await factory.CreateClientAsync(Users.Clerk);
        var product = await manager.CreateProductAsync(TestData.NewProduct(initialStock: 10));

        var response = await clerk.PostAsJsonAsync("/api/v1/orders", TestData.Order((product.Id, 3)));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var order = (await response.Content.ReadFromJsonAsync<OrderDto>())!;
        Assert.Equal("Placed", order.Status);
        Assert.Equal(3 * 19.99m, order.Total);
        Assert.Equal(Users.Clerk, order.PlacedBy);

        Assert.Equal(7, (await manager.GetProductAsync(product.Id)).StockOnHand);

        var ledger = await manager.GetFromJsonAsync<PagedResult<StockMovementDto>>($"/api/v1/products/{product.Id}/movements");
        Assert.Contains(ledger!.Items, m => m.Delta == -3 && m.Reason == "Sold" && m.Reference == order.Number);
    }

    [Fact]
    public async Task A_failed_order_changes_nothing_at_all()
    {
        var manager = await factory.CreateClientAsync(Users.Manager);
        var clerk = await factory.CreateClientAsync(Users.Clerk);
        var plenty = await manager.CreateProductAsync(TestData.NewProduct(initialStock: 10));
        var scarce = await manager.CreateProductAsync(TestData.NewProduct(initialStock: 2));

        var response = await clerk.PostAsJsonAsync("/api/v1/orders", TestData.Order((plenty.Id, 4), (scarce.Id, 5)));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(10, (await manager.GetProductAsync(plenty.Id)).StockOnHand);
        Assert.Equal(2, (await manager.GetProductAsync(scarce.Id)).StockOnHand);
    }

    [Fact]
    public async Task Cancelling_an_order_restores_the_stock_and_cannot_be_repeated()
    {
        var manager = await factory.CreateClientAsync(Users.Manager);
        var product = await manager.CreateProductAsync(TestData.NewProduct(initialStock: 10));
        var placed = await manager.PostAsJsonAsync("/api/v1/orders", TestData.Order((product.Id, 4)));
        var order = (await placed.Content.ReadFromJsonAsync<OrderDto>())!;

        var first = await manager.PostAsync($"/api/v1/orders/{order.Id}/cancel", content: null);
        var second = await manager.PostAsync($"/api/v1/orders/{order.Id}/cancel", content: null);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal("Cancelled", (await first.Content.ReadFromJsonAsync<OrderDto>())!.Status);
        Assert.Equal(10, (await manager.GetProductAsync(product.Id)).StockOnHand);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task A_clerk_can_place_but_not_cancel_an_order()
    {
        var manager = await factory.CreateClientAsync(Users.Manager);
        var clerk = await factory.CreateClientAsync(Users.Clerk);
        var product = await manager.CreateProductAsync(TestData.NewProduct());
        var placed = await clerk.PostAsJsonAsync("/api/v1/orders", TestData.Order((product.Id, 1)));
        var order = (await placed.Content.ReadFromJsonAsync<OrderDto>())!;

        var response = await clerk.PostAsync($"/api/v1/orders/{order.Id}/cancel", content: null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task An_order_for_an_unknown_product_returns_404()
    {
        var clerk = await factory.CreateClientAsync(Users.Clerk);

        var response = await clerk.PostAsJsonAsync("/api/v1/orders", TestData.Order((999_999, 1)));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task An_order_without_lines_returns_400()
    {
        var clerk = await factory.CreateClientAsync(Users.Clerk);

        var response = await clerk.PostAsJsonAsync("/api/v1/orders", TestData.Order());

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}