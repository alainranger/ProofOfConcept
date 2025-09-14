using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AspireAppSvelteSwapOrderItemPOC.WebApi.Model;

internal class Item
{
	public int ItemId { get; set; }
	public string ItemDesc { get; set; } = default!;
	
	// Position used to order items
	public int Position { get; set; }

	public override string ToString()
	{
		return ItemDesc;
	}
}
