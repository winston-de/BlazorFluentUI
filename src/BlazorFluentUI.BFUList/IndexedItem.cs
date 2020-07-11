using System;
using System.Collections.Generic;
using System.Text;

namespace BlazorFluentUI
{
    public class IndexedItem<TItem>
    {
        public TItem Item { get; set; }
        public int Index { get; set; }
    }
}
