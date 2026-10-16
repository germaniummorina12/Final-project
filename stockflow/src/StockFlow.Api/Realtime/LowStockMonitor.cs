using StockFlow.Application.Abstractions;

namespace StockFlow.Api.Realtime;

public sealed class LowStockMonitor(IServiceScopeFactory scopes, ILogger<LowStockMonitor> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);

        do
        {
            try
            {
                // A background service is a SINGLETON, but repositories are SCOPED. Create a scope per run.
                await using var scope = scopes.CreateAsyncScope();
                var products = scope.ServiceProvider.GetRequiredService<IProductRepository>();

                var low = await products.GetLowStockAsync(stoppingToken);
                if (low.Count > 0)
                {
                    logger.LogWarning("{Count} product(s) at or below the reorder level: {Skus}",
                        low.Count, string.Join(", ", low.Select(p => p.Sku)));
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "The low-stock check failed and will be retried");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}