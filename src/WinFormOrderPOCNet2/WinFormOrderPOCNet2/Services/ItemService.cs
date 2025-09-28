using System.Collections.Generic;
using WinFormOrderPOCNet2.Interfaces;
using WinFormOrderPOCNet2.Model;

namespace WinFormOrderPOCNet2.Services
{
    public class ItemService : IItemService
    {
        private readonly IItemRepository repository;
        private readonly List<Item> items;

        public ItemService(IItemRepository itemRepository)
        {
            repository = itemRepository;
            repository.Initialize();
            items = new List<Item>();
            LoadItems();
        }

        private void LoadItems()
        {
            items.Clear();
            items.AddRange(repository.GetAll());
        }

        public List<Item> GetItems()
        {
            return ListHelpers.OrderBy<Item, int>(items, delegate(Item item) { return item.ItemOrder; });
        }

        public void SwapItems(int indexA, int indexB)
        {
            if (indexA < 0 || indexA >= items.Count || indexB < 0 || indexB >= items.Count)
                return;

            Item temp = items[indexA];
            items[indexA] = items[indexB];
            items[indexB] = temp;
            RecalculateOrders();
        }

        public void MoveItemUp(int index)
        {
            if (index > 0)
            {
                SwapItems(index, index - 1);
            }
        }

        public void MoveItemDown(int index)
        {
            if (index < items.Count - 1 && index >= 0)
            {
                SwapItems(index, index + 1);
            }
        }

        public void MoveItemToTop(int index)
        {
            if (index <= 0 || index >= items.Count) return;
            Item item = items[index];
            items.RemoveAt(index);
            items.Insert(0, item);
            RecalculateOrders();
        }

        public void MoveItemToBottom(int index)
        {
            if (index < 0 || index >= items.Count - 1) return;
            Item item = items[index];
            items.RemoveAt(index);
            items.Add(item);
            RecalculateOrders();
        }

        public void SaveChanges()
        {
            repository.UpdateAllItemsOrder(items);
        }

        private void RecalculateOrders()
        {
            for (int i = 0; i < items.Count; i++)
            {
                items[i].ItemOrder = i + 1;
            }
        }
    }
}