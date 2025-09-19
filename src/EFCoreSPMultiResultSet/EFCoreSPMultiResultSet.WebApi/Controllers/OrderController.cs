using EFCoreSPMultiResultSet.WebApi.Model;
using EFCoreSPMultiResultSet.WebApi.Repository;
using Microsoft.AspNetCore.Mvc;

namespace EFCoreSPMultiResultSet.WebApi.Controllers;

// Controller for Order
public class OrderController(IOrderRepository orderRepository) : ControllerBase
{
    [HttpGet("api/orders/{orderId}")]
    public async Task<ActionResult<OrderNOrderItems>> GetOrderWithItems(int orderId)
    {
        var result = await orderRepository.GeOrderNOrderItemsById(orderId);
        if (result == null)
        {
            return NotFound();
        }
        return Ok(result);
    }
}