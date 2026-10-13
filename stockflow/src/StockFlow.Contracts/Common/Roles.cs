namespace StockFlow.Contracts.Common;

public static class Roles
{
    public const string Admin = "Admin";
    public const string Manager = "Manager";
    public const string Clerk = "Clerk";

    public static readonly string[] All = [Admin, Manager, Clerk];
}