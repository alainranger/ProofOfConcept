namespace EFCoreSPMultiResultSet.WebApi.Model;

public class OrderItem
{
    public int OrderItemID { get; set; }
    public int OrderID { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }

    // Navigation property
    public Order? Order { get; set; }
}