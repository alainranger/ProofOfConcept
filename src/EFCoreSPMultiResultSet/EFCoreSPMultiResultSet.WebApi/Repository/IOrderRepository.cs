using EFCoreSPMultiResultSet.WebApi.Model;

namespace EFCoreSPMultiResultSet.WebApi.Repository;

public interface IOrderRepository
{
    Task<OrderNOrderItems> GeOrderNOrderItemsById(int orderId);
}