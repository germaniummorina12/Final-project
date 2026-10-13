namespace StockFlow.Domain.Entities;

public class OrderLine : Entity
{
    public int OrderId { get; private set; }
    public int ProductId { get; private set; }
    public string ProductSku { get; private set; } = string.Empty;
    public string ProductName { get; private set; } = string.Empty;
    public decimal UnitPrice { get; private set; }
    public int Quantity { get; private set; }

    public decimal LineTotal => UnitPrice * Quantity;

    private OrderLine() { }

    internal OrderLine(Product product, int quantity)
    {
        ProductId = product.Id;
        ProductSku = product.Sku;
        ProductName = product.Name;
        UnitPrice = product.UnitPrice;
        Quantity = quantity;
    }

    internal void IncreaseQuantity(int quantity) => Quantity += quantity;
}