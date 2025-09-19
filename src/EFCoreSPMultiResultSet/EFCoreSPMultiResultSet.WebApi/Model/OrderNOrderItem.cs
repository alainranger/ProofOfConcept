using EFCoreSPMultiResultSet.WebApi.Model;

namespace EFCoreSPMultiResultSet.WebApi.Model;

public class OrderNOrderItems
{
    public Order? Order { get; set; }
    public IEnumerable<OrderItem> OrderItems { get; set; } = [];
}