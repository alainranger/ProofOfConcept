using System.Collections.Generic;
using WinFormOrderPOCNet2.Model;

namespace WinFormOrderPOCNet2.Interfaces
{
    public interface IItemService
    {
        List<Item> GetItems();
        void MoveItemUp(int index);
        void MoveItemDown(int index);
        void MoveItemToTop(int index);
        void MoveItemToBottom(int index);
        void SaveChanges();
        void SwapItems(int indexA, int indexB);
    }
}