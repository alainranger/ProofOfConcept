using AspireAppSvelteSwapOrderItemPOC.WebApi.Model;

using Microsoft.EntityFrameworkCore;

namespace AspireAppSvelteSwapOrderItemPOC.WebApi.Database;

internal class OrderbBContext(DbContextOptions<OrderbBContext> options) : DbContext(options)
{
	public DbSet<Item> Items { get; set; }
}
