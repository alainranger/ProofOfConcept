using System.ComponentModel.Design.Serialization;
using System.Data;
using System.Data.Common;
using EFCoreSPMultiResultSet.WebApi.Model;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace EFCoreSPMultiResultSet.WebApi.Repository;

public class OrderRepository(OrderContext context) : IOrderRepository
{
    public async Task<OrderNOrderItems> GeOrderNOrderItemsById(int orderId)
    {
        static OrderNOrderItems OrderNOrderItemsMapper(DbDataReader reader)
        {
            return new OrderNOrderItems
            {
                Order = reader.Translate<Order>(),
                OrderItems = reader.TranslateList<OrderItem>()
            };
        }

        var connection = context.Database.GetDbConnection();
        await connection.OpenAsync();

        var result = ExecuteReader(OrderNOrderItemsMapper, "[dbo].[GetOrderAndOrderItems]", orderId);
        return await Task.FromResult(result);
    }

    protected virtual T ExecuteReader<T>(Func<DbDataReader, T> mapEntities,
    string exec, params object[] parameters)
    {
        using var conn = new SqlConnection(context.Database.GetDbConnection().ConnectionString);
        using var command = new SqlCommand(exec, conn);
        conn.Open();
        command.Parameters.AddRange(parameters);
        command.CommandType = CommandType.StoredProcedure;
        try
        {
            using var reader = command.ExecuteReader();
            T data = mapEntities(reader);
            return data;
        }
        finally
        {
            conn.Close();
        }
    }
}
