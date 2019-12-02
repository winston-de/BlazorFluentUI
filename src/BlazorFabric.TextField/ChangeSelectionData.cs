using System;
using System.Collections.Generic;
using System.Text;

namespace BlazorFabric
{
    public class ChangeSelectionData
    {
        public InputChangeType ChangeType { get; set; }
        public int SelectionStart { get; set; }
        public int SelectionEnd { get; set; }
    }
}
