using System.Collections.Generic;
using WinFormOrderPOCNet2.Interfaces;
using WinFormOrderPOCNet2.Model;

namespace WinFormOrderPOCNet2.Repositories
{
    public class InMemoryItemRepository : IItemRepository
    {
        private readonly List<Item> items = new List<Item>();
        private int nextId = 1;

        public void Initialize()
        {
            if (items.Count == 0)
            {
                items.Add(new Item { ItemId = nextId++, ItemDesc = "Item A", ItemOrder = 1 });
                items.Add(new Item { ItemId = nextId++, ItemDesc = "Item B", ItemOrder = 2 });
                items.Add(new Item { ItemId = nextId++, ItemDesc = "Item C", ItemOrder = 3 });
            }
        }

        public List<Item> GetAll()
        {
            return new List<Item>(items);
        }

        public void UpdateItemOrder(Item item)
        {
            var existingItem = items.Find(delegate(Item i) { return i.ItemId == item.ItemId; });
            if (existingItem != null)
            {
                existingItem.ItemOrder = item.ItemOrder;
            }
        }

        public void UpdateAllItemsOrder(List<Item> updatedItems)
        {
            foreach (var updatedItem in updatedItems)
            {
                UpdateItemOrder(updatedItem);
            }
        }
    }
}