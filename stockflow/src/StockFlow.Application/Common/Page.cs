namespace StockFlow.Application.Common;

public sealed record Page<T>(IReadOnlyList<T> Items, int TotalCount);