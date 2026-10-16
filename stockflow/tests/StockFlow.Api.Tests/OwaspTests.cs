using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using StockFlow.Contracts.Auth;
using StockFlow.Contracts.Common;
using StockFlow.Contracts.Orders;
using StockFlow.Contracts.Products;

namespace StockFlow.Api.Tests;

public sealed class OwaspTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static StringContent Json(string body) => new(body, Encoding.UTF8, "application/json");

    private static string Base64Url(string text) =>
        Convert.ToBase64String(Encoding.UTF8.GetBytes(text)).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static string TokenOf(HttpClient client) => client.DefaultRequestHeaders.Authorization!.Parameter!;

    [Fact]
    public async Task A01_API5_a_clerk_cannot_adjust_stock_or_cancel_orders()
    {
        var manager = await factory.CreateClientAsync(Users.Manager);
        var clerk = await factory.CreateClientAsync(Users.Clerk);
        var product = await manager.CreateProductAsync(TestData.NewProduct());
        var placed = await manager.PostAsJsonAsync("/api/v1/orders", TestData.Order((product.Id, 1)));
        var order = (await placed.Content.ReadFromJsonAsync<OrderDto>())!;

        var adjust = await clerk.PostAsJsonAsync($"/api/v1/products/{product.Id}/stock",
            new { delta = 5, reason = "Received" });
        var cancel = await clerk.PostAsync($"/api/v1/orders/{order.Id}/cancel", content: null);

        Assert.Equal(HttpStatusCode.Forbidden, adjust.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, cancel.StatusCode);
    }

    [Fact(Skip = "Known gap, closed in E3 section 5. Remove Skip, watch it fail, apply the fix, watch it pass.")]
    public async Task A01_API1_a_clerk_cannot_read_an_order_placed_by_someone_else()
    {
        var manager = await factory.CreateClientAsync(Users.Manager);
        var clerk = await factory.CreateClientAsync(Users.Clerk);
        var product = await manager.CreateProductAsync(TestData.NewProduct());
        var placed = await manager.PostAsJsonAsync("/api/v1/orders", TestData.Order((product.Id, 1)));
        var order = (await placed.Content.ReadFromJsonAsync<OrderDto>())!;

        var response = await clerk.GetAsync($"/api/v1/orders/{order.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact(Skip = "Known gap, closed by the middleware in E3 section 7. Remove Skip once you added it.")]
    public async Task A02_API8_responses_carry_the_security_headers()
    {
        var response = await factory.CreateClient().GetAsync("/health");

        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("DENY", response.Headers.GetValues("X-Frame-Options").Single());
        Assert.True(response.Headers.Contains("Referrer-Policy"));
    }

    [Fact]
    public async Task A02_API9_there_are_no_forgotten_versions_or_debug_pages()
    {
        var manager = await factory.CreateClientAsync(Users.Manager);

        foreach (var path in new[] { "/api/products", "/api/v2/products", "/swagger", "/swagger/index.html", "/.env", "/appsettings.json" })
        {
            var response = await manager.GetAsync(path);
            Assert.True(response.StatusCode == HttpStatusCode.NotFound, $"{path} returned {(int)response.StatusCode}");
        }
    }

    [Fact]
    public async Task A07_API2_unknown_email_and_wrong_password_look_identical()
    {
        var client = factory.CreateClient();

        var unknown = await client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest { Email = "nobody@stockflow.local", Password = "whatever" });
        var wrong = await client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest { Email = Users.Manager, Password = "whatever" });

        Assert.Equal(HttpStatusCode.Unauthorized, unknown.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, wrong.StatusCode);

        string Detail(string body) => JsonDocument.Parse(body).RootElement.GetProperty("detail").GetString()!;
        Assert.Equal(Detail(await unknown.Content.ReadAsStringAsync()), Detail(await wrong.Content.ReadAsStringAsync()));
    }

    [Fact]
    public async Task A08_API2_a_tampered_token_is_rejected()
    {
        var manager = await factory.CreateClientAsync(Users.Manager);
        var token = TokenOf(manager);

        var tampered = token[..^1] + (token[^1] == 'A' ? 'B' : 'A');
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", tampered);

        var response = await client.GetAsync("/api/v1/products");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task A08_API2_a_token_with_alg_none_is_rejected()
    {
        var manager = await factory.CreateClientAsync(Users.Manager);
        var payload = TokenOf(manager).Split('.')[1];
        var unsigned = $"{Base64Url("""{"alg":"none","typ":"JWT"}""")}.{payload}.";
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", unsigned);

        var response = await client.GetAsync("/api/v1/products");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact(Skip = "Known gap, closed by the rate limiter in the SF12 exercise. Remove Skip once you added it.")]
    public async Task API2_the_sixth_wrong_password_in_a_minute_is_throttled()
    {
        var client = factory.CreateClient();
        HttpStatusCode last = default;

        for (var i = 0; i < 6; i++)
        {
            var response = await client.PostAsJsonAsync("/api/v1/auth/login",
                new LoginRequest { Email = Users.Clerk, Password = $"wrong-{i}" });
            last = response.StatusCode;
        }

        Assert.Equal(HttpStatusCode.TooManyRequests, last);
    }

    [Fact]
    public async Task A05_a_sql_injection_string_in_the_search_is_just_text()
    {
        var manager = await factory.CreateClientAsync(Users.Manager);
        var attack = Uri.EscapeDataString("'; DROP TABLE \"Products\"; --");

        var response = await manager.GetAsync($"/api/v1/products?search={attack}");
        var after = await manager.GetAsync("/api/v1/products");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(HttpStatusCode.OK, after.StatusCode);
    }

    [Fact]
    public async Task A05_script_in_a_product_name_is_stored_and_returned_as_data()
    {
        var manager = await factory.CreateClientAsync(Users.Manager);
        var request = TestData.NewProduct();
        const string name = "<img src=x onerror=alert(1)>";

        var response = await manager.PostAsJsonAsync("/api/v1/products", new
        {
            sku = request.Sku, name, categoryId = request.CategoryId,
            unitPrice = request.UnitPrice, initialStock = 1, reorderLevel = 0
        });
        var created = (await response.Content.ReadFromJsonAsync<ProductDto>())!;

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType!.MediaType);
        Assert.Equal(name, created.Name);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(10_001)]
    public async Task A06_API6_an_order_quantity_outside_the_allowed_range_is_refused(int quantity)
    {
        var manager = await factory.CreateClientAsync(Users.Manager);
        var product = await manager.CreateProductAsync(TestData.NewProduct());

        var response = await manager.PostAsJsonAsync("/api/v1/orders", TestData.Order((product.Id, quantity)));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(product.StockOnHand, (await manager.GetProductAsync(product.Id)).StockOnHand);
    }

    [Fact]
    public async Task API3_extra_fields_in_an_update_are_ignored()
    {
        var manager = await factory.CreateClientAsync(Users.Manager);
        var product = await manager.CreateProductAsync(TestData.NewProduct(initialStock: 10));

        var response = await manager.PutAsJsonAsync($"/api/v1/products/{product.Id}", new
        {
            name = "Renamed",
            categoryId = product.CategoryId,
            unitPrice = product.UnitPrice,
            reorderLevel = product.ReorderLevel,
            concurrencyStamp = product.ConcurrencyStamp,
            stockOnHand = 9999,
            isActive = false,
            sku = "HACKED-SKU"
        });
        var after = await manager.GetProductAsync(product.Id);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Renamed", after.Name);
        Assert.Equal(10, after.StockOnHand);
        Assert.True(after.IsActive);
        Assert.Equal(product.Sku, after.Sku);
    }

    [Fact]
    public async Task API4_a_huge_page_size_is_refused_or_clamped()
    {
        var manager = await factory.CreateClientAsync(Users.Manager);

        var products = await manager.GetAsync("/api/v1/products?pageSize=100000");
        var orders = await manager.GetAsync("/api/v1/orders?pageSize=100000");

        Assert.Equal(HttpStatusCode.BadRequest, products.StatusCode);
        Assert.Equal(HttpStatusCode.OK, orders.StatusCode);
        var page = (await orders.Content.ReadFromJsonAsync<PagedResult<OrderDto>>())!;
        Assert.True(page.PageSize <= 100);
    }

    [Theory]
    [InlineData("{")]
    [InlineData("[]")]
    [InlineData("null")]
    [InlineData("""{"customerId":"abc","lines":null}""")]
    [InlineData("""{"customerId":1,"lines":[{"productId":1,"quantity":99999999999999999999}]}""")]
    public async Task A10_malformed_input_gets_a_400_and_never_leaks_internals(string body)
    {
        var manager = await factory.CreateClientAsync(Users.Manager);

        var response = await manager.PostAsync("/api/v1/orders", Json(body));
        var text = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.DoesNotContain("StackTrace", text);
        Assert.DoesNotContain("   at ", text);
        Assert.DoesNotContain("StockFlow.", text);
    }
}