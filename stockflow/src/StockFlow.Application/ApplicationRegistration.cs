using Microsoft.Extensions.DependencyInjection;
using StockFlow.Application.Catalog;
using StockFlow.Application.Orders;
using StockFlow.Application.Products;

namespace StockFlow.Application;

public static class ApplicationRegistration
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IOrderService, OrderService>();
        return services;
    }
}