using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace BlazorFabric
{
    public class MaskValue
    {
        public int DisplayIndex { get; set; }
        public Regex Format { get; set; }
        public char? Value { get; set; }
    }
}
