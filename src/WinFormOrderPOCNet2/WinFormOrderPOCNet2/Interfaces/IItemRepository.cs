using System.Collections.Generic;
using WinFormOrderPOCNet2.Model;

namespace WinFormOrderPOCNet2.Interfaces
{
    public interface IItemRepository
    {
        List<Item> GetAll();
        void UpdateItemOrder(Item item);
        void UpdateAllItemsOrder(List<Item> items);
        void Initialize();
    }
}