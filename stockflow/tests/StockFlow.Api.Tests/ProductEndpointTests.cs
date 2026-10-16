using System.Net;
using System.Net.Http.Json;
using StockFlow.Contracts.Common;
using StockFlow.Contracts.Products;

namespace StockFlow.Api.Tests;

public sealed class ProductEndpointTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task Creating_a_product_returns_201_and_records_the_initial_stock_in_the_ledger()
    {
        var manager = await factory.CreateClientAsync(Users.Manager);

        var response = await manager.PostAsJsonAsync("/api/v1/products", TestData.NewProduct(initialStock: 10));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        var product = (await response.Content.ReadFromJsonAsync<ProductDto>())!;
        Assert.Equal(10, product.StockOnHand);

        var ledger = await manager.GetFromJsonAsync<PagedResult<StockMovementDto>>($"/api/v1/products/{product.Id}/movements");
        var movement = Assert.Single(ledger!.Items);
        Assert.Equal(10, movement.Delta);
        Assert.Equal("Received", movement.Reason);
    }

    [Fact]
    public async Task A_duplicate_sku_returns_409_as_a_problem_document()
    {
        var manager = await factory.CreateClientAsync(Users.Manager);
        var request = TestData.NewProduct();
        await manager.CreateProductAsync(request);

        var response = await manager.PostAsJsonAsync("/api/v1/products", request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task An_invalid_body_returns_400_naming_the_bad_field()
    {
        var manager = await factory.CreateClientAsync(Users.Manager);

        var response = await manager.PostAsJsonAsync("/api/v1/products", new CreateProductRequest());
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("sku", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Updating_with_a_stale_version_returns_409()
    {
        var manager = await factory.CreateClientAsync(Users.Manager);
        var created = await manager.CreateProductAsync(TestData.NewProduct());

        UpdateProductRequest Edit(string name, Guid stamp) => new()
        {
            Name = name,
            CategoryId = created.CategoryId,
            UnitPrice = created.UnitPrice,
            ReorderLevel = created.ReorderLevel,
            ConcurrencyStamp = stamp
        };

        var first = await manager.PutAsJsonAsync($"/api/v1/products/{created.Id}", Edit("First edit", created.ConcurrencyStamp));
        var second = await manager.PutAsJsonAsync($"/api/v1/products/{created.Id}", Edit("Second edit", created.ConcurrencyStamp));

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Search_filters_and_pages()
    {
        var manager = await factory.CreateClientAsync(Users.Manager);
        var tag = Guid.NewGuid().ToString("N")[..8];
        for (var i = 0; i < 3; i++)
        {
            await manager.CreateProductAsync(TestData.NewProduct(sku: $"S{tag}{i}"));
        }

        var page = await manager.GetFromJsonAsync<PagedResult<ProductDto>>($"/api/v1/products?search={tag}&page=1&pageSize=2");

        Assert.Equal(3, page!.TotalCount);
        Assert.Equal(2, page.Items.Count);
        Assert.Equal(2, page.TotalPages);
    }

    [Fact]
    public async Task Low_stock_lists_products_at_or_below_their_reorder_level()
    {
        var manager = await factory.CreateClientAsync(Users.Manager);
        var low = await manager.CreateProductAsync(TestData.NewProduct(initialStock: 1, reorderLevel: 5));
        var fine = await manager.CreateProductAsync(TestData.NewProduct(initialStock: 50, reorderLevel: 5));

        var list = await manager.GetFromJsonAsync<List<ProductDto>>("/api/v1/products/low-stock");

        Assert.Contains(list!, p => p.Id == low.Id);
        Assert.DoesNotContain(list!, p => p.Id == fine.Id);
    }

    [Fact]
    public async Task Removing_more_stock_than_exists_returns_409_and_changes_nothing()
    {
        var manager = await factory.CreateClientAsync(Users.Manager);
        var product = await manager.CreateProductAsync(TestData.NewProduct(initialStock: 3));

        var response = await manager.PostAsJsonAsync($"/api/v1/products/{product.Id}/stock",
            new AdjustStockRequest { Delta = -5, Reason = "Adjustment" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(3, (await manager.GetProductAsync(product.Id)).StockOnHand);
    }
}