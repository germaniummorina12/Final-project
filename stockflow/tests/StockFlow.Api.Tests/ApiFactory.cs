using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Npgsql;

namespace StockFlow.Api.Tests;

public sealed class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString =
        $"Host=localhost;Port=5432;Database=stockflow_test_{Guid.NewGuid():N};Username=postgres;Password=root";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development"); 
        builder.UseSetting("ConnectionStrings:Default", _connectionString);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        NpgsqlConnection.ClearAllPools();
        var database = new NpgsqlConnectionStringBuilder(_connectionString).Database;
        using var admin = new NpgsqlConnection("Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=root");
        admin.Open();
        using var drop = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{database}\" WITH (FORCE)", admin);
        drop.ExecuteNonQuery();
    }
}
