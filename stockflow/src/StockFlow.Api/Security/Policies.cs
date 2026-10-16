namespace StockFlow.Api.Security;

public static class Policies
{
    public const string ManageCatalog = "ManageCatalog";
    public const string AdminOnly = "AdminOnly";
    public const string PlaceOrders = "PlaceOrders";
}