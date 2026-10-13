namespace StockFlow.Contracts.Realtime;

public sealed record StockChangedMessage(int ProductId, string Sku, int StockOnHand, bool IsLowStock);