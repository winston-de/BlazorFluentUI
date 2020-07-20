using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DynamicData;
using DynamicData.Binding;
using DynamicData.Cache;
using DynamicData.Aggregation;
using System.Reactive.Linq;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using DynamicData.Binding;
using System.Reactive.Subjects;
using System.Reactive;

namespace BlazorFluentUI
{
    public partial class BFUGroupedList<TItem> : BFUComponentBase, IDisposable
    {
        //private IEnumerable<IGrouping<object, TItem>> groups;
        //private bool _isGrouped;
        private BFUList<GroupedListItem2<TItem>> listReference;

        private ReadOnlyObservableCollection<GroupedListItem<TItem>> dataItems;

        //private IEnumerable<Group<TItem,TKey>> _groups;

        private const double COMPACT_ROW_HEIGHT = 32;
        private const double ROW_HEIGHT = 42;

        private SourceCache<TItem, object> sourceCache;
        private SourceList<TItem> sourceList;
        private IDisposable sourceListSubscription;

        //private TItem _rootGroup;
        private IEnumerable<TItem> _itemsSource;

        private IDisposable _selectionSubscription;
        private IDisposable _transformedDisposable;

        [CascadingParameter]
        public BFUSelectionZone<TItem> SelectionZone { get; set; }

        [Parameter]
        public bool Compact { get; set; }

        /// <summary>
        /// GetKey must get a key that can be transformed into a unique string because the key will be written as HTML.  You can leave this null if your ItemsSource implements IList as the index will be used as a key.  
        /// </summary>
        [Parameter]
        public Func<TItem, object> GetKey { get; set; }

        [Parameter]
        public IList<Func<TItem, object>>? GroupBy { get; set; }

        [Parameter]
        public bool IsVirtualizing { get; set; } = true;

        [Parameter]
        public Func<TItem, MouseEventArgs, Task>? ItemClicked { get; set; }

        [Parameter]
        public IEnumerable<TItem>? ItemsSource { get; set; }

        [Parameter]
        public bool GroupSortDescending { get; set; }

        [Parameter]
        public RenderFragment<IndexedItem<GroupedListItem2<TItem>>>? ItemTemplate { get; set; }

        [Parameter]
        public EventCallback<GroupedListCollection<TItem>> OnGeneratedListItems { get; set; }

        [Parameter]
        public EventCallback<bool> OnGroupExpandedChanged { get; set; }

        [Parameter]
        public Func<bool> OnShouldVirtualize { get; set; } = () => true;

        [Parameter]
        public EventCallback<Viewport> OnViewportChanged { get; set; }

        //[Parameter]
        //public Selection<TItem> Selection { get; set; }

        [Parameter]
        public SelectionMode SelectionMode { get; set; } = SelectionMode.Single;

        [Parameter]
        public IList<Func<TItem, object>>? SortBy { get; set; } = null;

        [Parameter]
        public IList<bool>? SortDescending { get; set; } 

        [Parameter]
        public Func<TItem, IEnumerable<TItem>> SubGroupSelector { get; set; }


        private Func<TItem, object> getKeyInternal;
        private IDisposable sourceCacheSubscription;
        private ReadOnlyObservableCollection<GroupedListItem2<TItem>> groupedUIListItems;

        private IList<bool>? _sortDescending;
        private IList<Func<TItem, object>>? _sortBy;
        private BehaviorSubject<SortExpressionComparer<TItem>> sortExpressionComparer = new BehaviorSubject<SortExpressionComparer<TItem>>(new SortExpressionComparer<TItem>());
        private bool _groupSortDescending;
        private BehaviorSubject<SortExpressionComparer<GroupedListItem2<TItem>>> _groupSort = new BehaviorSubject<SortExpressionComparer<GroupedListItem2<TItem>>>(SortExpressionComparer<GroupedListItem2<TItem>>.Ascending(x=>x));
        private Subject<Unit> resorter = new Subject<Unit>();


        protected override Task OnInitializedAsync()
        {
            
            return base.OnInitializedAsync();
        }

        public void ToggleSelectAll()
        {
            if (SelectionZone.Selection.SelectedKeys.Count() != this.groupedUIListItems.Count())
            {
                //selectionZone.AddItems(ItemsSource);
                var list = new HashSet<object>();
                for (var i = 0; i < groupedUIListItems.Count(); i++)
                {
                    if (groupedUIListItems[i] is HeaderItem<TItem>)
                    {
                        list.Add(string.Join(',', groupedUIListItems[i].Children.Select(x=>getKeyInternal(x.Item)).ToArray()));
                    }
                    else
                    {
                        list.Add(getKeyInternal(groupedUIListItems[i].Item));
                    }
                }
                SelectionZone.AddKeys(list);
            }
            else
            {
                SelectionZone.ClearSelection();
            }
        }

        public bool ShouldAllBeSelected()
        {
            return SelectionZone.Selection.SelectedKeys.Count() == groupedUIListItems.Count() && groupedUIListItems.Any();
        }

        private string GetKeyForHeader(GroupedListItem2<TItem> header)
        {
            return string.Join(',', header.Children.Select(x => getKeyInternal(x.Item)).ToArray());
        }

        private void OnHeaderClicked(IndexedItem<GroupedListItem2<TItem>> indexedItem)
        {
            if (SelectionZone != null)
            {
                // Doesn't seem to be any difference in the behavior for clicking the Header vs the checkmark in the header.
                //does selection contain this item already?
                var headerKey = GetKeyForHeader(indexedItem.Item);
                if (SelectionZone.Selection.SelectedKeys.Contains(headerKey))
                {
                    var listToDeselect = new List<object>();
                    //deselect it and all possible children
                    listToDeselect.Add(headerKey);
                    if (dataItems.Count - 1 > indexedItem.Index)  // there are more items to check
                    {
                        for (var i = indexedItem.Index + 1; i < dataItems.Count; i++)
                        {
                            if (dataItems[i].Depth > indexedItem.Item.Depth)
                            {
                                listToDeselect.Add(dataItems[i].Key);
                            }
                            else
                                break;
                        }
                    }
                    SelectionZone.RemoveKeys(listToDeselect);
                    //deselect it and all children
                    //var items = SubGroupSelector(headerItem.Item)?.RecursiveSelect<TItem, TItem>(r => SubGroupSelector(r), i => i).Append(headerItem.Item);
                    //SelectionZone.RemoveItems(items);
                }
                else
                {
                    var listToSelect = new List<object>();
                    //select it and all possible children
                    listToSelect.Add(headerKey);
                    if (dataItems.Count - 1 > indexedItem.Index)  // there are more items to check
                    {
                        for (var i = indexedItem.Index + 1; i < dataItems.Count; i++)
                        {
                            if (dataItems[i].Depth > indexedItem.Item.Depth)
                            {
                                listToSelect.Add(dataItems[i].Key);
                            }
                            else
                                break;
                        }
                    }
                    SelectionZone.AddKeys(listToSelect);
                    //select it and all children
                    //var items = SubGroupSelector(headerItem.Item)?.RecursiveSelect<TItem, TItem>(r => SubGroupSelector(r), i => i).Append(headerItem.Item);
                    //SelectionZone.AddItems(items);
                }
            }
        }

        private void OnHeaderToggled(IndexedItem<GroupedListItem2<TItem>> indexedItem)
        {
            if (SelectionZone != null)
            {
                // Doesn't seem to be any difference in the behavior for clicking the Header vs the checkmark in the header.
                //does selection contain this item already?
                var headerKey = GetKeyForHeader(indexedItem.Item);
                if (SelectionZone.Selection.SelectedKeys.Contains(headerKey))
                {
                    var listToDeselect = new List<object>();
                    //deselect it and all possible children
                    listToDeselect.Add(headerKey);
                    if (dataItems.Count - 1 > indexedItem.Index)  // there are more items to check
                    {
                        for (var i = indexedItem.Index + 1; i < dataItems.Count; i++)
                        {
                            if (dataItems[i].Depth > indexedItem.Item.Depth)
                            {
                                listToDeselect.Add(dataItems[i].Key);
                            }
                            else
                                break;
                        }
                    }
                    SelectionZone.RemoveKeys(listToDeselect);
                    //deselect it and all children
                    //var items = SubGroupSelector(headerItem.Item)?.RecursiveSelect<TItem, TItem>(r => SubGroupSelector(r), i => i).Append(headerItem.Item);
                    //SelectionZone.RemoveItems(items);
                }
                else
                {
                    var listToSelect = new List<object>();
                    //select it and all possible children
                    listToSelect.Add(headerKey);
                    if (dataItems.Count - 1 > indexedItem.Index)  // there are more items to check
                    {
                        for (var i = indexedItem.Index + 1; i < dataItems.Count; i++)
                        {
                            if (dataItems[i].Depth > indexedItem.Item.Depth)
                            {
                                listToSelect.Add(dataItems[i].Key);
                            }
                            else
                                break;
                        }
                    }
                    SelectionZone.AddKeys(listToSelect);
                    //select it and all children
                    //var items = SubGroupSelector(headerItem.Item)?.RecursiveSelect<TItem, TItem>(r => SubGroupSelector(r), i => i).Append(headerItem.Item);
                    //SelectionZone.AddItems(items);
                }
            }
        }

        //private System.Collections.Generic.ICollection<GroupedListItem<TItem>> GetChildrenRecursive(GroupedListItem<TItem> item)
        //{
        //    var groupedItems = new System.Collections.Generic.List<GroupedListItem<TItem>>();
        //    foreach (var child in item.Children)
        //    {
        //        groupedItems.Add(child);
        //        var subItems = GetChildrenRecursive(child);
        //        groupedItems.Add(subItems);
        //    }
        //    return groupedItems;
        //}

        //private System.Collections.Generic.IEnumerable<GroupedListItem<TItem>> GetS(IEnumerable<TItem> items)
        //{
        //    var groupedItems = new System.Collections.Generic.List<GroupedListItem<TItem>>();
        //    foreach (var item in items)
        //    {
        //        var foundItem = dataItems.FirstOrDefault(x => x.Item.Equals(item));
        //        if (foundItem != null)
        //            groupedItems.Add(foundItem);

        //        var moreItems = SubGroupSelector.Invoke(item);
        //        if (moreItems != null)
        //            groupedItems.AddRange(GetS(moreItems));
        //    }
        //    return groupedItems;
        //}

        public void ForceUpdate()
        {
            _itemsSource = null;

            StateHasChanged();
        }

        protected override async Task OnParametersSetAsync()
        {
            if (GetKey == null)
            {
                if (!(ItemsSource is IList<TItem>))
                {
                    throw new Exception("ItemsSource must either have GetKey set to point to a key value for each item OR ItemsSource must be an indexable list that implements IList.");
                }
                getKeyInternal = item => ItemsSource.IndexOf(item);
            }
            else
            {
                getKeyInternal = GetKey;
            }


            if (SortBy != _sortBy || SortDescending != _sortDescending)
            {
                _sortBy = SortBy;
                _sortDescending = SortDescending;
                if (SortBy != null)
                {
                    var index = 0;
                    foreach (var sortFunc in SortBy)
                    {
                        if (SortDescending != null && SortDescending.ElementAt(index) != null && SortDescending.ElementAt(index) == true)
                            sortExpressionComparer.OnNext(SortExpressionComparer<TItem>.Descending(sortFunc.ConvertToIComparable()));
                        else
                            sortExpressionComparer.OnNext(SortExpressionComparer<TItem>.Ascending(sortFunc.ConvertToIComparable()));
                        index++;
                    }
                }
                else
                {
                    sortExpressionComparer.OnNext(new SortExpressionComparer<TItem>());
                }
                //if (_groupSortDescending)
                //    _groupSort.OnNext(SortExpressionComparer<GroupedListItem2<TItem>>.Descending(x => x));
                //else
                //    _groupSort.OnNext(SortExpressionComparer<GroupedListItem2<TItem>>.Ascending(x => x));
            }

            if (GroupSortDescending != _groupSortDescending)
            {
                _groupSortDescending = GroupSortDescending;
                if (_groupSortDescending)
                    _groupSort.OnNext(SortExpressionComparer<GroupedListItem2<TItem>>.Descending(x => x));
                else 
                    _groupSort.OnNext(SortExpressionComparer<GroupedListItem2<TItem>>.Ascending(x => x));

            }

            if (GroupBy != null)
            {
                if (ItemsSource != null && !ItemsSource.Equals(_itemsSource))
                {
                    _itemsSource = ItemsSource;
                    CreateSourceCache();
                    //sourceCache.AddOrUpdate(_itemsSource);
                    sourceList.AddRange(_itemsSource);
                }
            }
            else if (SubGroupSelector != null)
            {
                ////if (ItemsSource != null && !ItemsSource.Equals(_itemsSource))
                ////if (RootGroup != null && !RootGroup.Equals(_rootGroup))

                //if (ItemsSource != null && !ItemsSource.Equals(_itemsSource))
                //{
                //    //dispose old subscriptions
                //    _transformedDisposable?.Dispose();

                //    _itemsSource = ItemsSource;
                //    if (_itemsSource != null)
                //    {
                //        var changeSet = _itemsSource.AsObservableChangeSet();
                //        System.Collections.Generic.List<HeaderItem<TItem>> headersList = new System.Collections.Generic.List<HeaderItem<TItem>>();
                //        Dictionary<int, int> depthIndex = new Dictionary<int, int>();

                //        var rootIndex = 0;

                //        var transformedChangeSet = changeSet.TransformMany<GroupedListItem<TItem>, TItem>((x) =>

                //        {
                //            var header = new HeaderItem<TItem>(x, null, rootIndex++, 0, GroupTitleSelector);
                //            headersList.Add(header);
                //            var children = SubGroupSelector(x)
                //                .RecursiveSelect<TItem, GroupedListItem<TItem>>(
                //                r => SubGroupSelector(r),
                //                            (s, index, depth) =>
                //                            {
                //                                if (!depthIndex.ContainsKey(depth))
                //                                    depthIndex[depth] = 0;
                //                                var parent = headersList.FirstOrDefault(header => header.Depth == depth - 1 && SubGroupSelector(header.Item).Contains(s));
                //                                if (SubGroupSelector(s) == null || SubGroupSelector(s).Count() == 0)
                //                                {
                //                                    var item = new PlainItem<TItem>(s, parent, index, depth);
                //                                    parent?.Children.Add(item);
                //                                    return item;
                //                                }
                //                                else
                //                                {
                //                                    var header = new HeaderItem<TItem>(s, parent, index, depth, GroupTitleSelector);
                //                                    headersList.Add(header);
                //                                    parent?.Children.Add(header);
                //                                    return header;
                //                                }
                //                            },
                //                            1);
                //            return Enumerable.Repeat(header, 1).Concat(children);
                //        });


                //        _transformedDisposable = transformedChangeSet
                //            .AutoRefreshOnObservable(x => x.IsVisibleObservable)
                //            //.Filter(x => x.IsVisible)
                //            .Sort(new GroupedListItemComparer<TItem>())
                //            .Bind(out dataItems)
                //            .Do(x =>
                //            {
                //                this.OnGeneratedListItems.InvokeAsync(new GroupedListCollection<TItem> { GroupedListItems = dataItems });
                //            })
                //            .Subscribe();

                //    }
                //}
            }



            await base.OnParametersSetAsync();
            
        }

        protected override Task OnAfterRenderAsync(bool firstRender)
        {
            Debug.WriteLine($"There are {groupedUIListItems.Count} items to render");
            return base.OnAfterRenderAsync(firstRender);
        }

        private void CreateSourceCache()
        {
            sourceCacheSubscription?.Dispose();
            sourceCacheSubscription = null;

            sourceListSubscription?.Dispose();
            sourceListSubscription = null;

            if (_itemsSource == null)
            {
                return;
            }
            
            //sourceCache = new SourceCache<TItem, object>(getKeyInternal);

            //var expression = sourceCache.Connect();

            //var items = expression.FlatGroup<TItem, object>(GroupBy, 0, new List<object>(), SortBy, SortDescending)
            //    .Batch(TimeSpan.FromSeconds(0.2))
            //    .Sort(SortExpressionComparer<GroupedListItem2<TItem>>.Ascending(x=>x));
                        
            //sourceCacheSubscription = items.Bind(out groupedUIListItems)
            //    .Do(_ => InvokeAsync(StateHasChanged))
            //    .Do(_ => Debug.WriteLine($"There are {groupedUIListItems.Count} items to render."))
            //    .Subscribe();

            sourceList = new SourceList<TItem>();
            sourceListSubscription = sourceList.Connect()
                .FlatGroup<TItem>(GroupBy, 0, new List<object>(), sortExpressionComparer)
                .AutoRefreshOnObservable(x=> sortExpressionComparer)
                .Sort(_groupSort)
                .Bind(out groupedUIListItems)
                .Do(_ => InvokeAsync(StateHasChanged))
                //.Do(_ => Debug.WriteLine($"There are {groupedUIListItems.Count} items to render."))
                .Subscribe();

        }

       
        //public void SelectAll()
        //{
        //    SelectionZone.AddItems(dataItems.)
        //}


        public void Dispose()
        {
            _transformedDisposable?.Dispose();
            _selectionSubscription?.Dispose();
        }
    }
}
