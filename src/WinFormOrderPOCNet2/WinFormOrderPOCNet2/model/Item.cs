namespace WinFormOrderPOCNet2.Model
{
	public class Item
	{
        public int ItemId { get; set; }
        public string ItemDesc { get; set; }
        public int ItemOrder { get; set; }

        public override string ToString()
        {
            return ItemDesc;
        }
    }
}
