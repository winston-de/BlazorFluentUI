using System;
using System.Collections.Generic;
using System.Text;

namespace BlazorFluentUI
{
    public class Selection<TItem>
    {
        private IEnumerable<TItem> _items;
        public IEnumerable<TItem> SelectedItems
        {
            get => _items;
            set => _items = value;
        }

        private IEnumerable<int> _indices;
        public IEnumerable<int> SelectedIndices
        {
            get => _indices;
            set => _indices = value;
        }
                

        public Selection()
        {
            _items = new List<TItem>();
            _indices = new List<int>();
        }

        public Selection(IEnumerable<TItem> items)
        {
            _items = items;
            _indices = new List<int>();
        }

        public Selection(IEnumerable<int> indices)
        {
            _items = new List<TItem>();
            _indices = indices;
        }


        public void ClearSelection()
        {
            _items = new List<TItem>();
            _indices = new List<int>();
        }
    }
}
