using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StockFlow.Contracts.Common;
using StockFlow.Domain.Entities;
using StockFlow.Infrastructure.Auth;

namespace StockFlow.Infrastructure.Persistence;

public static class DbInitializer
{
    private const string DefaultDemoPassword = "StockFlow#2026";

    public static async Task InitializeAsync(IServiceProvider services, IConfiguration configuration)
    {
        await using var scope = services.CreateAsyncScope();
        var provider = scope.ServiceProvider;

        var db = provider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();

        await SeedRolesAsync(provider.GetRequiredService<RoleManager<IdentityRole>>());

        if (configuration.GetValue<bool>("Seed:DemoUsers"))
        {
            var password = configuration["Seed:DemoPassword"] ?? DefaultDemoPassword;
            var users = provider.GetRequiredService<UserManager<AppUser>>();
            await SeedUserAsync(users, "admin@stockflow.local", password, Roles.Admin);
            await SeedUserAsync(users, "manager@stockflow.local", password, Roles.Manager);
            await SeedUserAsync(users, "clerk@stockflow.local", password, Roles.Clerk);
        }

        if (configuration.GetValue<bool>("Seed:SampleData"))
        {
            await SeedSampleDataAsync(db, provider.GetRequiredService<TimeProvider>());
        }
    }

    private static async Task SeedRolesAsync(RoleManager<IdentityRole> roles)
    {
        foreach (var role in Roles.All)
        {
            if (!await roles.RoleExistsAsync(role))
            {
                await roles.CreateAsync(new IdentityRole(role));
            }
        }
    }

    private static async Task SeedUserAsync(UserManager<AppUser> users, string email, string password, string role)
    {
        if (await users.FindByEmailAsync(email) is not null)
        {
            return;
        }

        var user = new AppUser { UserName = email, Email = email, EmailConfirmed = true };
        var result = await users.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                $"Could not create {email}: {string.Join("; ", result.Errors.Select(e => e.Description))}");
        }

        await users.AddToRoleAsync(user, role);
    }

    private static async Task SeedSampleDataAsync(AppDbContext db, TimeProvider clock)
    {
        if (await db.Categories.AnyAsync())
        {
            return;
        }

        var now = clock.GetUtcNow().UtcDateTime;

        var peripherals = new Category("Peripherals");
        var monitors = new Category("Monitors");
        var cables = new Category("Cables");
        db.Categories.AddRange(peripherals, monitors, cables);
        await db.SaveChangesAsync();

        var products = new[]
        {
            (Product: new Product("KB-001", "Mechanical Keyboard", peripherals.Id, 89.90m, 5), Stock: 25),
            (Product: new Product("MS-001", "Wireless Mouse", peripherals.Id, 34.50m, 8), Stock: 40),
            (Product: new Product("MN-27", "27-inch Monitor", monitors.Id, 249.00m, 3), Stock: 6),
            (Product: new Product("HUB-C", "USB-C Hub", peripherals.Id, 45.00m, 10), Stock: 4),
            (Product: new Product("CB-HDMI", "HDMI Cable 2 m", cables.Id, 12.99m, 20), Stock: 100)
        };

        foreach (var (product, stock) in products)
        {
            db.Products.Add(product);
            db.StockMovements.Add(product.Receive(stock, "seed", now, "Initial stock"));
        }

        db.Customers.AddRange(
            new Customer("Acme Corp", "orders@acme.example"),
            new Customer("Globex Ltd", "buy@globex.example"),
            new Customer("Initech", "it@initech.example"));

        await db.SaveChangesAsync();
    }
}