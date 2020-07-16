using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;

namespace BlazorFluentUI
{
    public class Selection<TItem>
    {
        private IEnumerable<TItem> _selectedItems;
        public IEnumerable<TItem> SelectedItems
        {
            get => _selectedItems;
            set => _selectedItems = value;
        }

        private IEnumerable<TItem> itemsSource;
        private IList<TItem> listSource;
        private Dictionary<int, TItem> pairs = new Dictionary<int, TItem>();

        private IEnumerable<GroupedListItem<TItem>> groupedItemsSource;
        private IList<GroupedListItem<TItem>> listgroupedSource;
        private Dictionary<int, GroupedListItem<TItem>> groupedPairs = new Dictionary<int, GroupedListItem<TItem>>();

        private ICollection<int> _selectedIndices;
        public ICollection<int> SelectedIndices
        {
            get => _selectedIndices;
            set => _selectedIndices = value;
        }

        private ICollection<object> _selectedKeys;
        public ICollection<object> SelectedKeys
        {
            get => _selectedKeys;
            set => _selectedKeys = value;
        }

        private BehaviorSubject<ICollection<object>> selectedKeysSubject;
        /// <summary>
        /// Bypasses the dependence on Blazor's render cycle to propagate data.
        /// </summary>
        public IObservable<ICollection<object>> SelectedKeysObservable { get; private set; }


        private BehaviorSubject<ICollection<int>> selectedIndicesSubject;

        /// <summary>
        /// Bypasses the dependence on Blazor's render cycle to propagate data.
        /// </summary>
        public IObservable<ICollection<int>> SelectedIndicesObservable { get; private set; }


        public Selection()
        {
            _selectedItems = new List<TItem>();
            _selectedIndices = new HashSet<int>();
            _selectedKeys = new HashSet<object>();

            selectedKeysSubject = new BehaviorSubject<ICollection<object>>(_selectedKeys);
            SelectedKeysObservable = selectedKeysSubject.AsObservable();

            selectedIndicesSubject = new BehaviorSubject<ICollection<int>>(_selectedIndices);
            SelectedIndicesObservable = selectedIndicesSubject.AsObservable();
        }

        public Selection<TItem> SetSelectedKeys(ICollection<object> selectedKeys)
        {
            SelectedKeys = selectedKeys;            
            selectedKeysSubject.OnNext(selectedKeys);
            return this;
        }

        public Selection<TItem> SetSelectedIndices(ICollection<int> selectedIndices)
        {
            SelectedIndices = selectedIndices;
            if (!typeof(TItem).IsValueType && itemsSource != null)
            {
                pairs.Clear();
                foreach (var index in selectedIndices)
                {
                     pairs.Add(index, itemsSource.ElementAt(index));
                }
            }
            //required for groupedlistitems
            if (!typeof(TItem).IsValueType && groupedItemsSource != null)
            {
                groupedPairs.Clear();
                foreach (var index in selectedIndices)
                {
                    groupedPairs.Add(index, groupedItemsSource.ElementAt(index));
                }
            }

            selectedIndicesSubject.OnNext(_selectedIndices);
            return this;
        }


        public void SetItems(IEnumerable<TItem> items)
        {
            if (this.itemsSource is System.Collections.Specialized.INotifyCollectionChanged)
            {
                (this.itemsSource as System.Collections.Specialized.INotifyCollectionChanged)!.CollectionChanged -= ListBase_CollectionChanged;
            }

            itemsSource = items;
            if (items is IList<TItem>)
                listSource = (IList<TItem>)items;
            else
                listSource = items.ToList();

            if (itemsSource is System.Collections.Specialized.INotifyCollectionChanged)
            {
                (itemsSource as System.Collections.Specialized.INotifyCollectionChanged)!.CollectionChanged += ListBase_CollectionChanged;
            }
        }

        public void SetGroupedItems(IEnumerable<GroupedListItem<TItem>> groupedItems)
        {
            if (this.groupedItemsSource is System.Collections.Specialized.INotifyCollectionChanged)
            {
                (this.groupedItemsSource as System.Collections.Specialized.INotifyCollectionChanged)!.CollectionChanged -= Grouped_CollectionChanged;
            }

            groupedItemsSource = groupedItems;
            if (groupedItems is IList<GroupedListItem<TItem>>)
                listgroupedSource = (IList<GroupedListItem<TItem>>)groupedItems;
            else
                listgroupedSource = groupedItems.ToList();

            if (groupedItemsSource is System.Collections.Specialized.INotifyCollectionChanged)
            {
                (groupedItemsSource as System.Collections.Specialized.INotifyCollectionChanged)!.CollectionChanged += Grouped_CollectionChanged;
            }
        }

        public void ClearSelection()
        {
            _selectedKeys = new HashSet<object>();
            selectedKeysSubject.OnNext(_selectedKeys);
            _selectedItems = new List<TItem>();
            _selectedIndices = new HashSet<int>();
            selectedIndicesSubject.OnNext(_selectedIndices);
        }

        private void ListBase_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            RecalculateIndices();
        }

        private void Grouped_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            RecalculateGroupedIndices();
        }

        private void RecalculateIndices()
        {
            if (!typeof(TItem).IsValueType && itemsSource != null)
            {
                var newPairs = new Dictionary<int, TItem>();
                foreach (var pair in pairs)
                {
                    var newIndex = listSource.IndexOf(pair.Value);
                    newPairs.Add(newIndex, pair.Value);
                }
                pairs = newPairs;
                _selectedIndices = pairs.Select(x => x.Key).ToList();
                selectedIndicesSubject.OnNext(_selectedIndices);
            }
        }

        private void RecalculateGroupedIndices()
        {
            if (!typeof(TItem).IsValueType && groupedItemsSource != null)
            {
                var newPairs = new Dictionary<int, GroupedListItem<TItem>>();
                foreach (var pair in groupedPairs)
                {
                    var newIndex = listgroupedSource.IndexOf(pair.Value);
                    newPairs.Add(newIndex, pair.Value);
                }
                groupedPairs = newPairs;
                _selectedIndices = groupedPairs.Select(x => x.Key).ToList();
                selectedIndicesSubject.OnNext(_selectedIndices);
            }
        }
    }
}
