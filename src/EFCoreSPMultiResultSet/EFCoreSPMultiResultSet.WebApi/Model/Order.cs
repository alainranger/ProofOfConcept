namespace EFCoreSPMultiResultSet.WebApi.Model;

public class Order
{
    public int OrderID { get; set; }
    public DateTime OrderDate { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }

    // Navigation property
    public List<OrderItem> OrderItems { get; set; } = new();
}