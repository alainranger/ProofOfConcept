using AspireAppSvelteSwapOrderItemPOC.WebApi.Model;

using Microsoft.EntityFrameworkCore;

namespace AspireAppSvelteSwapOrderItemPOC.WebApi.Database;

public class SeedData
{
	public static void Initialize(IServiceProvider serviceProvider)
	{
		using var context = new OrderbBContext(
			serviceProvider.GetRequiredService<DbContextOptions<OrderbBContext>>());

		// Look for any games.
		if (context.Items.Any())
		{
			return;   // DB has been seeded
		}

		context.Items.AddRange(
		   new Item
		   {
			   ItemId = 1,
			   ItemDesc	= "The Legend of Zelda: Breath of the Wild",
			   Position = 1
		   },
		   new Item
		   {
			   ItemId = 2,
			   ItemDesc	= "Super Mario Odyssey",
			   Position = 2
		   },
		   new Item
		   {
			   ItemId = 3,
			   ItemDesc	= "Mario Kart 8 Deluxe",
			   Position = 3
		   }
	   );

		context.SaveChanges();
	}
}
