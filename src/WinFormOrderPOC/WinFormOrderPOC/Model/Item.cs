using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WinFormOrderPOC.Model
{
	internal class Item
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
