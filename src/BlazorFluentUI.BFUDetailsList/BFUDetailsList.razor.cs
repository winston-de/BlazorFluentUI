using DynamicData;
using DynamicData.Binding;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace BlazorFluentUI
{

    public partial class BFUDetailsList<TItem> : BFUComponentBase, INotifyPropertyChanged
    {
        private IEnumerable<BFUDetailsRowColumn<TItem>> _columns;

        [Parameter]
        public CheckboxVisibility CheckboxVisibility { get; set; } = CheckboxVisibility.OnHover;

        [Parameter]
        public IEnumerable<BFUDetailsRowColumn<TItem>> Columns { get => _columns; set { if (_columns == value) return; else { _columns = value; OnPropertyChanged(); } } }

        [Parameter]
        public bool Compact { get; set; }

        [Parameter]
        public bool DisableSelectionZone { get; set; }

        [Parameter]
        public bool EnterModalSelectionOnTouch { get; set; }

        [Parameter]
        public RenderFragment FooterTemplate { get; set; }

        /// <summary>
        /// GetKey must get a key that can be transformed into a unique string because the key will be written as HTML.  You can leave this null if your ItemsSource implements IList as the index will be used as a key.  
        /// </summary>
        [Parameter]
        public Func<TItem,object> GetKey { get; set; }

        [Parameter]
        public IList<Func<TItem, object>> GroupBy { get; set; }


        [Parameter]
        public Func<TItem, string> GroupTitleSelector { get; set; }

        [Parameter]
        public RenderFragment HeaderTemplate { get; set; }

        [Parameter]
        public bool IsHeaderVisible { get; set; } = true;

        [Parameter]
        public bool IsVirtualizing { get; set; } = true;

        [Parameter]
        public IEnumerable<TItem> ItemsSource { get; set; }

        [Parameter]
        public DetailsListLayoutMode LayoutMode { get; set; }

        [Parameter]
        public EventCallback<TItem> OnItemContextMenu { get; set; }

        [Parameter]
        public EventCallback<TItem> OnItemInvoked { get; set; }

        [Parameter]
        public EventCallback<ColumnResizedArgs<TItem>> OnColumnResized { get; set; }

        [Parameter]
        public RenderFragment<IndexedItem<TItem>>? RowTemplate { get; set; }

        [Parameter]
        public Selection<TItem> Selection { get; set; } = new Selection<TItem>();

        [Parameter]
        public EventCallback<Selection<TItem>> SelectionChanged { get; set; }

        [Parameter]
        public SelectionMode SelectionMode { get; set; }

        [Parameter]
        public bool SelectionPreservedOnEmptyClick { get; set; }

        

        //State
        int focusedItemIndex;
        double _lastWidth = -1;
        SelectionMode _lastSelectionMode;
        Viewport _lastViewport;
        Viewport _viewport;
        private IEnumerable<BFUDetailsRowColumn<TItem>> _adjustedColumns = Enumerable.Empty<BFUDetailsRowColumn<TItem>>();
        const double MIN_COLUMN_WIDTH = 100;

        Dictionary<string, double> _columnOverrides = new Dictionary<string, double>();

        BFUGroupedList<TItem>? groupedList;
        BFUList<TItem>? list;
        BFUSelectionZone<TItem>? selectionZone;

        protected bool isAllSelected;
        private bool shouldRender = true;

        private IReadOnlyDictionary<string, object> lastParameters = null;

        protected SelectAllVisibility selectAllVisibility = SelectAllVisibility.None;

        private SourceCache<TItem, object> sourceCache;
        private ReadOnlyObservableCollection<TItem> items;
        private ReadOnlyObservableCollection<GroupedListItem2<TItem>> groupedUIItems;

        private IObservable<Func<TItem, bool>>? DynamicDescriptionFilter;
        private IEnumerable<TItem>? itemsSource;
        private IDisposable? sourceCacheSubscription;
        private Subject<Unit> applyFilter = new Subject<Unit>();

        private Func<TItem, object> getKeyInternal;

        private IList<Func<TItem, object>>? groupSortSelectors;
        private IList<bool>? groupSortDescendingList;

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public BFUDetailsList()
        {
            
            
        }

        
        private void OnColumnClick(BFUDetailsRowColumn<TItem> column)
        {
            if (column.PropType.GetInterface("IComparable") != null)
            {
                if (column.IsSorted && column.IsSortedDescending)
                {
                    column.IsSortedDescending= false;
                }
                else if (column.IsSorted)
                {
                    column.IsSortedDescending = true;
                }
                else
                {
                    column.IsSorted = true;
                }
            }
            foreach (var col in Columns)
            {
                if (col != column)
                {
                    col.IsSorted = false;
                    col.IsSortedDescending = false;
                }
            }
        }

        public void ForceUpdate()
        {
            //groupedList?.ForceUpdate();
        }

        protected override bool ShouldRender()
        {
            Debug.WriteLine($"DetailsList should render: {shouldRender}");
            if (!shouldRender)
            {
                shouldRender = true;
                //return false;
            }
            return true;
        }

        public override Task SetParametersAsync(ParameterView parameters)
        {
            shouldRender = false;

            var dictParameters = parameters.ToDictionary();
            if (lastParameters == null)
            {
                shouldRender = true;
            }
            else
            {
                var differences = dictParameters.Where(entry =>
                {
                    return !lastParameters[entry.Key].Equals(entry.Value);
                }
                ).ToDictionary(entry => entry.Key, entry => entry.Value);

                if (differences.Count > 0)
                {
                    shouldRender = true;
                }
            }
            lastParameters = dictParameters;

            if (_viewport != null && _viewport != _lastViewport)
            {
                AdjustColumns(
                    parameters.GetValueOrDefault<IEnumerable<TItem>>("ItemsSource"),
                    parameters.GetValueOrDefault<DetailsListLayoutMode>("LayoutMode"),
                    parameters.GetValueOrDefault<SelectionMode>("SelectionMode"),
                    parameters.GetValueOrDefault<CheckboxVisibility>("CheckboxVisibility"),
                    parameters.GetValueOrDefault<IEnumerable<BFUDetailsRowColumn<TItem>>>("Columns"),
                    true
                    );
            }

            var selectionMode = parameters.GetValueOrDefault<SelectionMode>("SelectionMode");
            if (selectionMode == SelectionMode.None)
            {
                selectAllVisibility = SelectAllVisibility.None;
            }
            else if (selectionMode == SelectionMode.Single)
            {
                selectAllVisibility = SelectAllVisibility.Hidden;
            }
            else if (selectionMode == SelectionMode.Multiple)
            {
                //disable if collapsed groups
                //TBD!

                selectAllVisibility = SelectAllVisibility.Visible;
            }

            if (parameters.GetValueOrDefault<CheckboxVisibility>("CheckboxVisibility") == CheckboxVisibility.Hidden)
            {
                selectAllVisibility = SelectAllVisibility.None;
            }

            //var subGroupSelector = parameters.GetValueOrDefault<Func<TItem, IEnumerable<TItem>>>("SubGroupSelector");
            


            return base.SetParametersAsync(parameters);
        }

        protected override Task OnParametersSetAsync()
        {
            if (GroupBy == null && ItemsSource != null)
            {
                selectionZone?.SetItemsSource(ItemsSource);
            }

            //Setup SourceCache to pull from GetKey or from IList index

            if (GetKey == null)
            {
                if (!(itemsSource is IList<TItem>))
                {
                    throw new Exception("ItemsSource must either have GetKey set to point to a key value for each item OR ItemsSource must be an indexable list that implements IList.");
                }
                getKeyInternal = item => itemsSource.IndexOf(item);

            }
            else
            {
                getKeyInternal = GetKey;
            }

            if (ItemsSource != itemsSource)
            {
                itemsSource = ItemsSource;
                CreateSourceCache();
                sourceCache.AddOrUpdate(itemsSource);
            }

            return base.OnParametersSetAsync();
        }

        public void Filter()
        {
            //reset filter icons
            foreach (var col in Columns)
                col.IsFiltered = false;
            applyFilter.OnNext(Unit.Default);
        }

        private void CreateSourceCache()
        {
            sourceCacheSubscription?.Dispose();
            sourceCacheSubscription = null;

            if (itemsSource == null)
            {
                return;
            }

            sourceCache = new SourceCache<TItem, object>(getKeyInternal);

            //Setup observable for INotifyPropertyChanged
            var propertyChanged = Observable.FromEvent<PropertyChangedEventHandler, PropertyChangedEventArgs>(
              handler =>
              {
                  PropertyChangedEventHandler changed = (sender, e) => handler(e);
                  return changed;
              },
              handler => this.PropertyChanged += handler,
              handler => this.PropertyChanged -= handler);

            //watch for changes to any properties and pick out changes to Columns, need to return an initial value in case Columns was already set.
            var columnsObservable = Observable.Return(new PropertyChangedEventArgs("Columns"))
                .Merge(propertyChanged)
                .Where(x => x.PropertyName == "Columns")
                .SelectMany(prop =>
                {
                    // now watch for changes to the Columns object properties and return an initial value so that any following logic can be setup 
                    return this.Columns.Aggregate(Observable.Empty<PropertyChangedEventArgs>(), (x, y) => x.Merge(y.PropertyChangedObs));
                });


            //Setup filter expression observable
            var filterExpression = Observable.Return(new PropertyChangedEventArgs("FilterPredicate")).Merge(columnsObservable).Do(x => Debug.WriteLine($"prop:{x.PropertyName} ")).Where(colProp => colProp.PropertyName == "FilterPredicate").Select(row =>
              {
                  var columnsWithFilters = this.Columns
                    .Where(row => row.FilterPredicate != null).ToList();

                  return (Func<TItem, bool>)(item =>
                   {
                       foreach (var col in columnsWithFilters)
                       {
                           if (!col.FilterPredicate(col.FieldSelector(item)))
                           {
                               col.IsFiltered = true;
                               return false;
                           }
                       }
                       return true;
                   });

              });

            

            var sortExpression = Observable.Return(new PropertyChangedEventArgs("IsSorted")).Merge(columnsObservable).Where(colProp => colProp.PropertyName == "IsSorted" || colProp.PropertyName == "IsSortedDescending").Select(x =>
                {
                    var sort = this.Columns.Where(x => x.IsSorted);

                    SortExpressionComparer<TItem> sortChain;
                    if (sort.Count() > 1)
                    {
                        var first = sort.Take(1).First();

                        var rest = sort.Skip(1);
                        sortChain = rest.Aggregate(first.IsSortedDescending ?
                            SortExpressionComparer<TItem>.Descending(first.FieldSelector.ConvertToIComparable()) :
                            SortExpressionComparer<TItem>.Ascending(first.FieldSelector.ConvertToIComparable()),
                            (x, y) => y.IsSortedDescending ?
                            x.ThenByDescending(y.FieldSelector.ConvertToIComparable()) :
                            x.ThenByAscending(y.FieldSelector.ConvertToIComparable()));
                    }
                    else if (sort.Count() == 1)
                    {
                        var first = sort.Take(1).First();
                        sortChain = first.IsSortedDescending ?
                            SortExpressionComparer<TItem>.Descending(first.FieldSelector.ConvertToIComparable()) :
                            SortExpressionComparer<TItem>.Ascending(first.FieldSelector.ConvertToIComparable());
                    }
                    else
                    {
                        sortChain = new SortExpressionComparer<TItem>();
                    }

                    return sortChain;
                });

            Observable.Return(new PropertyChangedEventArgs("IsSorted")).Merge(columnsObservable).Where(colProp => colProp.PropertyName == "IsSorted" || colProp.PropertyName == "IsSortedDescending").Select(x =>
            {
                var sort = this.Columns.Where(x => x.IsSorted);
                if (sort.Count() > 0)
                {
                    return sort.Select(x=>x.FieldSelector).ToList();
                }
                else
                {
                    return null;
                }
            }).Subscribe(x =>
            {
                groupSortSelectors = x;
            });

            Observable.Return(new PropertyChangedEventArgs("IsSorted")).Merge(columnsObservable).Where(colProp => colProp.PropertyName == "IsSorted" || colProp.PropertyName == "IsSortedDescending").Select(x =>
            {
                var sort = this.Columns.Where(x => x.IsSorted);
                if (sort.Count() > 0)
                {
                    return sort.Select(x => x.IsSortedDescending).ToList();
                }
                else
                {
                    return null;
                }
            }).Subscribe(x =>
            {
                groupSortDescendingList = x;
            });

            // bind sourceCache to renderable list
            var preBindExpression = sourceCache.Connect()
               .Filter(filterExpression, applyFilter)
               .Sort(sortExpression);

            //if (GroupBy != null)
            //{
            //    var firstGroupBy = GroupBy.First();

            //    var groups = preBindExpression.Group(firstGroupBy);
            //    var index = 0;
            //    var depth = 0;

            //    // using or and sorting groups later
            //    var headerItems = groups.Transform(x =>
            //    {                    
            //        return new HeaderItem2<TItem>(default, null, depth, x.Key.ToString()) as GroupedListItem2<TItem>;
            //    });

            //    //assume only 1 grouping operation
            //    var subItems = groups.MergeMany(group =>
            //    {
            //        return group.Cache.Connect().Transform(item => new PlainItem2<TItem>(item, group, depth + 1) as GroupedListItem2<TItem>);
            //    });

            //    var items = headerItems.Or(subItems);


            //    //var uiItems = groups.TransformMany(x =>
            //    //{
            //    //    //assume only 1 grouping operation
            //    //    var header = new HeaderItem2<TItem>(default, null, index++, depth, x.Key.ToString());
            //    //    var subIndex = 0;
            //    //    x.Cache.Connect()
                        
            //    //        .Transform(x => new PlainItem2<TItem>(x, header, subIndex++, depth + 1))
            //    //        .Bind(out var subItems)
            //    //        .Subscribe();

            //    //    return Enumerable.Concat<GroupedListItem2<TItem>>(Enumerable.Repeat(header,1), subItems);

            //    //    // Need to make this recursive, then change GroupedList to accept raw ui items with generic TItem
            //    //    // Move this to the GroupedList instead.  Have DetailsList output plain sorted/filtered items but send the GroupBy parameter

            //    //    //try to do FAKE grouping.  Sort by group selector, but don't change items.  

            //    //}, x => x.Key);
                

            //        //.TransformMany(x=>x.Cache)
            //        //.Transform(group=>new GroupContainer<TItem>(group, GroupBy.Skip(1).ToList()))
            //        //uiItems
            //        //.Bind(out groupedUIItems)
            //        //.Do(x => StateHasChanged())
            //        //.Subscribe();
            //}
            //else
            {
                sourceCacheSubscription = preBindExpression.Bind(out items)
                    .Do(x => StateHasChanged())
                    .Subscribe();
            }
        }


        //private void OnGroupedListGeneratedItems(GroupedListCollection<TItem> groupedListItems)
        //{
        //    selectionZone?.SetGroupedItemsSource(groupedListItems.GroupedListItems);
        //    //return Task.CompletedTask;
        //}

        protected override Task OnAfterRenderAsync(bool firstRender)
        {
            return base.OnAfterRenderAsync(firstRender);
        }

        private void OnHeaderKeyDown(KeyboardEventArgs keyboardEventArgs)
        {
            // this was attached in the ms-DetailsList-headerWrapper div.  When holding Ctrl nothing happens (since it's a meta key), but if you click while holding Ctrl, a large number of keydown events is sent to this handler and freezes the UI. 
        }

        private void OnContentKeyDown(KeyboardEventArgs keyboardEventArgs)
        {
            // this was attached in the ms-DetailsList-contentWrapper div.  When holding Ctrl nothing happens (since it's a meta key), but if you click while holding Ctrl, a large number of keydown events is sent to this handler and freezes the UI. 
        }

        private bool ShouldAllBeSelected()
        {
            if (GroupBy == null)
            {
                return Selection.SelectedIndices.Count() == items.Count() && items.Any();
                //return Selection.SelectedItems.Count() == ItemsSource.Count() && ItemsSource.Any();
            }
            else
            {
                ////source is grouped... need to recursively select them all.

                //var flattenedItems = ItemsSource?.SelectManyRecursive(x => SubGroupSelector(x));
                //if (flattenedItems == null)
                //    return false;

                //return flattenedItems.Count() == Selection.SelectedItems.Count() && flattenedItems.Any();
                if (groupedList == null)
                    return false;
                return groupedList.ShouldAllBeSelected();
            }
        }

        private void OnAllSelected()
        {
            if (GroupBy == null)
            {
                if (Selection.SelectedKeys.Count() != this.items.Count())
                {
                    //selectionZone.AddItems(ItemsSource);
                    var list = new List<object>();
                    for (var i=0; i< items.Count(); i++)
                    {
                        list.Add(getKeyInternal(items[i]));
                    }
                    selectionZone.AddKeys(list);
                }
                else
                {
                    selectionZone.ClearSelection();
                }
            }
            else
            {
                groupedList.ToggleSelectAll();
                ////source is grouped... need to recursively select them all.
                //var flattenedItems = this.ItemsSource?.SelectManyRecursive(x => SubGroupSelector(x));
                //if (flattenedItems.Count() != Selection.SelectedItems.Count())
                //{
                //    selectionZone.AddItems(flattenedItems);
                //}
                //else
                //{
                //    selectionZone.ClearSelection();
                //}
            }
        }

        private void ViewportChangedHandler(Viewport viewport)
        {
            _lastViewport = _viewport;
            _viewport = viewport;
            //Debug.WriteLine($"Viewport changed: {viewport.ScrollWidth}");
            if (_viewport != null)
                AdjustColumns(items, LayoutMode, SelectionMode, CheckboxVisibility, Columns, true);
        }

        private void AdjustColumns(IEnumerable<TItem> newItems, DetailsListLayoutMode newLayoutMode, SelectionMode newSelectionMode, CheckboxVisibility newCheckboxVisibility, IEnumerable<BFUDetailsRowColumn<TItem>> newColumns, bool forceUpdate, int resizingColumnIndex = -1)
        {
            _adjustedColumns = GetAdjustedColumns(newItems, newLayoutMode, newSelectionMode, newCheckboxVisibility, newColumns, forceUpdate, resizingColumnIndex);
        }

        private IEnumerable<BFUDetailsRowColumn<TItem>> GetAdjustedColumns(IEnumerable<TItem> newItems, DetailsListLayoutMode newLayoutMode, SelectionMode newSelectionMode, CheckboxVisibility newCheckboxVisibility, IEnumerable<BFUDetailsRowColumn<TItem>> newColumns, bool forceUpdate, int resizingColumnIndex)
        {
            var columns = Columns.EmptyIfNull();
            var lastWidth = _lastWidth;
            var lastSelectionMode = _lastSelectionMode;

            if (!forceUpdate && _lastViewport.ScrollWidth == _viewport.ScrollWidth && SelectionMode == newSelectionMode && (Columns == null || newColumns == Columns))
                return Enumerable.Empty<BFUDetailsRowColumn<TItem>>();

            // skipping default column builder... user must provide columns always

            IEnumerable<BFUDetailsRowColumn<TItem>> adjustedColumns = null;

            if (LayoutMode == DetailsListLayoutMode.FixedColumns)
            {
                adjustedColumns = GetFixedColumns(newColumns);

                foreach (var col in adjustedColumns)
                    _columnOverrides[col.Key] = col.CalculatedWidth;
            }
            else
            {
                if (resizingColumnIndex != -1)
                {
                    adjustedColumns = GetJustifiedColumnsAfterResize(newColumns, newCheckboxVisibility, newSelectionMode, _viewport.ScrollWidth, resizingColumnIndex);
                }
                else
                {
                    adjustedColumns = GetJustifiedColumns(newColumns, newCheckboxVisibility, newSelectionMode, _viewport.ScrollWidth, resizingColumnIndex);
                }

                foreach (var col in adjustedColumns)
                {
                    _columnOverrides[col.Key] = col.CalculatedWidth;
                }
            }



            return adjustedColumns;
        }

        private IEnumerable<BFUDetailsRowColumn<TItem>> GetFixedColumns(IEnumerable<BFUDetailsRowColumn<TItem>> newColumns)
        {
            foreach (var col in newColumns)
            {
                col.CalculatedWidth = !double.IsNaN(col.MaxWidth) ? col.MaxWidth : (!double.IsNaN(col.MinWidth) ? col.MinWidth : MIN_COLUMN_WIDTH);
            }
            return newColumns;
        }

        private IEnumerable<BFUDetailsRowColumn<TItem>> GetJustifiedColumnsAfterResize(IEnumerable<BFUDetailsRowColumn<TItem>> newColumns, CheckboxVisibility newCheckboxVisibility, SelectionMode newSelectionMode, double viewportWidth, int resizingColumnIndex)
        {
            var fixedColumns = newColumns.Take(resizingColumnIndex);
            foreach (var col in fixedColumns)
            {
                if (_columnOverrides.TryGetValue(col.Key, out var overridenWidth))
                    col.CalculatedWidth = overridenWidth;
                else
                    col.CalculatedWidth = double.NaN;
            }

            int count = 0;
            var fixedWidth = fixedColumns.Aggregate<BFUDetailsRowColumn<TItem>, double, double>(0, (total, column) => total + GetPaddedWidth(column, ++count == 0), x => x);

            var remainingColumns = newColumns.Skip(resizingColumnIndex).Take(newColumns.Count() - resizingColumnIndex);
            var remainingWidth = viewportWidth - fixedWidth;

            var adjustedColumns = GetJustifiedColumns(remainingColumns, newCheckboxVisibility, newSelectionMode, remainingWidth, resizingColumnIndex);

            return Enumerable.Concat(fixedColumns, adjustedColumns);
        }

        private IEnumerable<BFUDetailsRowColumn<TItem>> GetJustifiedColumns(IEnumerable<BFUDetailsRowColumn<TItem>> newColumns, CheckboxVisibility newCheckboxVisibility, SelectionMode newSelectionMode, double viewportWidth, int resizingColumnIndex)
        {
            var rowCheckWidth = newSelectionMode != SelectionMode.None && newCheckboxVisibility != CheckboxVisibility.Hidden ? 48 : 0;  //DetailsRowCheckbox width
            var groupExpandedWidth = 0; //skipping this for now.
            double totalWidth = 0;
            var availableWidth = viewportWidth - (rowCheckWidth + groupExpandedWidth);
            int count = 0;

            System.Collections.Generic.List<BFUDetailsRowColumn<TItem>> adjustedColumns = new System.Collections.Generic.List<BFUDetailsRowColumn<TItem>>();
            foreach (var col in newColumns)
            {
                adjustedColumns.Add(col);
                col.CalculatedWidth = !double.IsNaN(col.MinWidth) ? col.MinWidth : 100;
                if (_columnOverrides.TryGetValue(col.Key, out var overridenWidth))
                    col.CalculatedWidth = overridenWidth;

                var isFirst = count + resizingColumnIndex == 0;
                totalWidth += GetPaddedWidth(col, isFirst);
            }

            var lastIndex = adjustedColumns.Count() - 1;

            // Shrink or remove collapsable columns.
            while (lastIndex > 0 && totalWidth > availableWidth)
            {
                var col = adjustedColumns.ElementAt(lastIndex);
                var minWidth = !double.IsNaN(col.MinWidth) ? col.MinWidth : 100;
                var overflowWidth = totalWidth - availableWidth;

                if (col.CalculatedWidth - minWidth >= overflowWidth || !col.IsCollapsible)
                {
                    var originalWidth = col.CalculatedWidth;
                    col.CalculatedWidth = Math.Max(col.CalculatedWidth - overflowWidth, minWidth);
                    totalWidth -= originalWidth - col.CalculatedWidth;
                }
                else
                {
                    totalWidth -= GetPaddedWidth(col, false);
                    adjustedColumns.RemoveRange(lastIndex, 1);
                }
                lastIndex--;
            }

            //Then expand columns starting at the beginning, until we've filled the width.
            for (var i = 0; i < adjustedColumns.Count && totalWidth < availableWidth; i++)
            {
                var col = adjustedColumns[i];
                var isLast = i == adjustedColumns.Count - 1;
                var hasOverrides = _columnOverrides.TryGetValue(col.Key, out var overrides);
                if (hasOverrides && !isLast)
                    continue;

                var spaceLeft = availableWidth - totalWidth;
                double increment = 0;
                if (isLast)
                    increment = spaceLeft;
                else
                {
                    var maxWidth = col.MaxWidth;
                    var minWidth = !double.IsNaN(col.MinWidth) ? col.MinWidth : (!double.IsNaN(col.MaxWidth) ? col.MaxWidth : 100);
                    increment = !double.IsNaN(maxWidth) ? Math.Min(spaceLeft, maxWidth - minWidth) : spaceLeft;
                }

                col.CalculatedWidth += increment;
                totalWidth += increment;
            }

            return adjustedColumns;
        }

        private double GetPaddedWidth(BFUDetailsRowColumn<TItem> column, bool isFirst)
        {
            return column.CalculatedWidth +
                    BFUDetailsRow<TItem>.CellLeftPadding +
                    BFUDetailsRow<TItem>.CellRightPadding +
                    (column.IsPadded ? BFUDetailsRow<TItem>.CellExtraRightPadding : 0);
        }

        private void OnColumnResizedInternal(ColumnResizedArgs<TItem> columnResizedArgs)
        {
            OnColumnResized.InvokeAsync(columnResizedArgs);

            _columnOverrides[columnResizedArgs.Column.Key] = columnResizedArgs.NewWidth;
            AdjustColumns(items, LayoutMode, SelectionMode, CheckboxVisibility, Columns, true, columnResizedArgs.ColumnIndex);
        }

        private void OnColumnAutoResized(ItemContainer<BFUDetailsRowColumn<TItem>> itemContainer)
        {
            // TO-DO - will require measuring row cells, jsinterop
        }
    }
}
