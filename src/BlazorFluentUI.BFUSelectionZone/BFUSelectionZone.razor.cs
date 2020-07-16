using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;


namespace BlazorFluentUI
{
    public partial class BFUSelectionZone<TItem> : BFUComponentBase
    {
        [Parameter]
        public RenderFragment? ChildContent { get; set; }

        [Parameter]
        public bool DisableAutoSelectOnInputElements { get; set; }

        [Parameter]
        public bool DisableRenderOnSelectionChanged { get; set; } = false;

        [Parameter]
        public bool EnterModalOnTouch { get; set; }

        [Parameter]
        public bool IsSelectedOnFocus { get; set; } = true;
        
        [Parameter]
        public EventCallback<TItem> OnItemContextMenu { get; set; }

        [Parameter]
        public EventCallback<TItem> OnItemInvoked { get; set; }

        [Parameter]
        public Selection<TItem>? Selection { get; set; }

        [Parameter]
        public EventCallback<Selection<TItem>> SelectionChanged { get; set; }

        [Parameter]
        public SelectionMode SelectionMode { get; set; }

        [Parameter]
        public bool SelectionPreservedOnEmptyClick { get; set; }


        private IEnumerable<TItem>? _itemsSource;  //need to assume this iteration of ItemsSource has temporary order.
        private IEnumerable<GroupedListItem<TItem>>? _groupedItemsSource;  //need to assume this iteration of ItemsSource has temporary order.
        //private Dictionary<Guid, TItem> dic;
        //private HashSet<Guid> selectedKeys;
        //private HashSet<int> selectedIndices;// = new HashSet<TItem>();
        private HashSet<object> selectedKeys;

        //private BehaviorSubject<ICollection<int>> selectedIndicesSubject;

        //public IObservable<ICollection<int>> SelectedIndicesObservable => Selection.SelectedIndicesObservable; //{ get; private set; }
        public IObservable<ICollection<object>> SelectedKeysObservable => Selection.SelectedKeysObservable; //{ get; private set; }

        private bool doNotRenderOnce = false;

        protected override bool ShouldRender()
        {
            Debug.WriteLine("SelectionZone Should render called");
            if (doNotRenderOnce && DisableRenderOnSelectionChanged)
            {
                doNotRenderOnce = false;
                return false;
            }
            else
                doNotRenderOnce = false;

            return true;
            //return base.ShouldRender();
        }

        public BFUSelectionZone()
        {
            //dic = new Dictionary<Guid, TItem>();
            //selectedKeys = new HashSet<Guid>();
            selectedKeys = new HashSet<object>();
            //selectedIndices = new HashSet<int>();
            //selectedIndicesSubject = new BehaviorSubject<ICollection<int>>(selectedIndices);
            //SelectedIndicesObservable = selectedIndicesSubject.AsObservable();
        }

        //protected override void OnInitialized()
        //{
        //    selectedItemsSubject = new BehaviorSubject<ICollection<TItem>>(selectedItems);
        //    SelectedItemsObservable = selectedItemsSubject.AsObservable();
        //    base.OnInitialized();
        //}

        protected override async Task OnParametersSetAsync()
        {           
            if (Selection != null && Selection.SelectedKeys != selectedKeys)
            {
                //selectedIndices = new System.Collections.Generic.HashSet<int>(Selection.SelectedIndices);
                selectedKeys = new HashSet<object>(Selection.SelectedKeys);
                //selectedItemsSubject.OnNext(selectedItems);
                //StateHasChanged();
            }

            if (SelectionMode == SelectionMode.Single && selectedKeys.Count() > 1)
            {
                selectedKeys.Clear();
                Selection?.SetSelectedKeys(selectedKeys);
                //selectedIndices.Clear();
                //Selection?.SetSelectedIndices(selectedIndices);
                await SelectionChanged.InvokeAsync(Selection);
            }
            else if (SelectionMode == SelectionMode.None && selectedKeys.Count() > 0)
            {
                selectedKeys.Clear();
                Selection?.SetSelectedKeys(selectedKeys);
                //selectedIndices.Clear();
                //Selection?.SetSelectedIndices(selectedIndices);
                await SelectionChanged.InvokeAsync(Selection);
            }
            await base.OnParametersSetAsync();
        }

        protected override Task OnAfterRenderAsync(bool firstRender)
        {
            return base.OnAfterRenderAsync(firstRender);
        }


        //private void CreateKeys()
        //{
        //    dic.Clear();
        //    if (itemsSource != null)
        //    {
        //        foreach (var item in itemsSource)
        //        {
        //            dic.Add(Guid.NewGuid(), item);
        //        }
        //    }
        //}

        public TItem GetItemAtIndex(int index)
        {
            return _itemsSource.ElementAt(index);
        }

        public IEnumerable<int> GetSelectedIndices()
        {
            if (Selection == null)
                return new List<int>();
            return Selection.SelectedIndices;
            
        }

        public void SetItemsSource(IEnumerable<TItem> itemsSource)
        {
            if (_itemsSource != itemsSource)
            {
                
                _itemsSource = itemsSource;
                Selection.SetItems(_itemsSource);
            }
        }

        public void SetGroupedItemsSource(IEnumerable<GroupedListItem<TItem>> groupedItemsSource)
        {
            if (_groupedItemsSource != groupedItemsSource)
            {

                _groupedItemsSource = groupedItemsSource;
                Selection.SetGroupedItems(_groupedItemsSource);
            }
        }


        /// <summary>
        /// For DetailsRow
        /// </summary>
        /// <param name="item"></param>
        /// <param name="asSingle">On click, force list to select one even if set to multiple</param>
        //public void SelectItem(TItem item, bool asSingle=false)
        //{
        //    bool hasChanged = false;
        //    if (SelectionMode == SelectionMode.Multiple && !asSingle)
        //    {
        //        hasChanged = true;
        //        if (selectedIndices.Contains(item))
        //            selectedIndices.Remove(item);
        //        else
        //            selectedIndices.Add(item);
        //    }
        //    else if (SelectionMode == SelectionMode.Multiple && asSingle)
        //    {
        //        //same as single except we need to clear other items if they are selected, too
        //        hasChanged = true;
        //        selectedIndices.Clear();
        //        selectedIndices.Add(item);
        //    }
        //    else if (SelectionMode == SelectionMode.Single)
        //    {
        //        if (!selectedIndices.Contains(item))
        //        {
        //            hasChanged = true;
        //            selectedIndices.Clear();
        //            selectedIndices.Add(item);
        //        }
        //    }

        //    if (hasChanged)
        //    {
        //        doNotRenderOnce = true;
        //        selectedIndicesSubject.OnNext(selectedIndices);
        //        SelectionChanged.InvokeAsync(new Selection<TItem>(selectedIndices));
        //    }
        //}
        public void SelectKey(object key, bool asSingle=false)
        {
            bool hasChanged = false;
            if (SelectionMode == SelectionMode.Multiple && !asSingle)
            {
                hasChanged = true;
                if (selectedKeys.Contains(key))
                    selectedKeys.Remove(key);
                else
                    selectedKeys.Add(key);
            }
            else if (SelectionMode == SelectionMode.Multiple && asSingle)
            {
                //same as single except we need to clear other items if they are selected, too
                hasChanged = true;
                selectedKeys.Clear();
                selectedKeys.Add(key);
            }
            else if (SelectionMode == SelectionMode.Single)
            {
                if (!selectedKeys.Contains(key))
                {
                    hasChanged = true;
                    selectedKeys.Clear();
                    selectedKeys.Add(key);
                }
            }

            if (hasChanged)
            {
                doNotRenderOnce = true;
                //selectedIndicesSubject.OnNext(selectedIndices);
                SelectionChanged.InvokeAsync(Selection.SetSelectedKeys(selectedKeys));
            }
        }

        //public void SelectIndex(int index, bool asSingle = false)
        //{
        //    bool hasChanged = false;
        //    if (SelectionMode == SelectionMode.Multiple && !asSingle)
        //    {
        //        hasChanged = true;
        //        if (selectedIndices.Contains(index))
        //            selectedIndices.Remove(index);
        //        else
        //            selectedIndices.Add(index);
        //    }
        //    else if (SelectionMode == SelectionMode.Multiple && asSingle)
        //    {
        //        //same as single except we need to clear other items if they are selected, too
        //        hasChanged = true;
        //        selectedIndices.Clear();
        //        selectedIndices.Add(index);
        //    }
        //    else if (SelectionMode == SelectionMode.Single)
        //    {
        //        if (!selectedIndices.Contains(index))
        //        {
        //            hasChanged = true;
        //            selectedIndices.Clear();
        //            selectedIndices.Add(index);
        //        }
        //    }

        //    if (hasChanged)
        //    {
        //        doNotRenderOnce = true;
        //        //selectedIndicesSubject.OnNext(selectedIndices);
        //        SelectionChanged.InvokeAsync(Selection.SetSelectedIndices(selectedIndices));
        //    }
        //}

        //public void AddItems(IEnumerable<TItem> items)
        //{
        //    foreach (var item in items)
        //    {
        //        if (!selectedIndices.Contains(item))
        //            selectedIndices.Add(item);
        //    }

        //    if (items != null && items.Count() > 0)
        //    {
        //        doNotRenderOnce = true;
        //        selectedIndicesSubject.OnNext(selectedIndices);
        //        SelectionChanged.InvokeAsync(new Selection<TItem>(selectedIndices));
        //    }
        //}

        public void AddKeys(IEnumerable<object> keys)
        {
            foreach (var key in keys)
            {
                if (!selectedKeys.Contains(key))
                    selectedKeys.Add(key);
            }

            if (keys != null && keys.Count() > 0)
            {
                doNotRenderOnce = true;
                //selectedIndicesSubject.OnNext(selectedIndices);
                SelectionChanged.InvokeAsync(Selection.SetSelectedKeys(selectedKeys));
            }
        }

        //public void AddIndices(IEnumerable<int> indices)
        //{
        //    foreach (var index in indices)
        //    {
        //        if (!selectedIndices.Contains(index))
        //            selectedIndices.Add(index);
        //    }

        //    if (indices != null && indices.Count() > 0)
        //    {
        //        doNotRenderOnce = true;
        //        //selectedIndicesSubject.OnNext(selectedIndices);
        //        SelectionChanged.InvokeAsync(Selection.SetSelectedIndices(selectedIndices));
        //    }
        //}

        //public void RemoveItems(IEnumerable<TItem> items)
        //{
        //    foreach (var item in items)
        //    {
        //        selectedIndices.Remove(item);
        //    }

        //    if (items != null && items.Count() > 0)
        //    {
        //        doNotRenderOnce = true;
        //        selectedIndicesSubject.OnNext(selectedIndices);
        //        SelectionChanged.InvokeAsync(new Selection<TItem>(selectedIndices));
        //    }
        //}

        public void RemoveKeys(IEnumerable<object> keys)
        {
            foreach (var key in keys)
            {
                selectedKeys.Remove(key);
            }

            if (keys != null && keys.Count() > 0)
            {
                doNotRenderOnce = true;
                //selectedIndicesSubject.OnNext(selectedIndices);
                SelectionChanged.InvokeAsync(Selection.SetSelectedKeys(selectedKeys));
            }
        }

        //public void RemoveIndices(IEnumerable<int> indices)
        //{
        //    foreach (var index in indices)
        //    {
        //        selectedIndices.Remove(index);
        //    }

        //    if (indices != null && indices.Count() > 0)
        //    {
        //        doNotRenderOnce = true;
        //        //selectedIndicesSubject.OnNext(selectedIndices);
        //        SelectionChanged.InvokeAsync(Selection.SetSelectedIndices(selectedIndices));
        //    }
        //}

        //public void AddAndRemoveItems(IEnumerable<TItem> itemsToAdd, IEnumerable<TItem> itemsToRemove)
        //{
        //    foreach (var item in itemsToAdd)
        //    {
        //        if (!selectedIndices.Contains(item))
        //            selectedIndices.Add(item);
        //    }
        //    foreach (var item in itemsToRemove)
        //    {
        //        selectedIndices.Remove(item);
        //    }

        //    if ((itemsToAdd != null && itemsToAdd.Count() > 0) || (itemsToRemove != null && itemsToRemove.Count() > 0))
        //    {
        //        doNotRenderOnce = true;
        //        selectedIndicesSubject.OnNext(selectedIndices);
        //        SelectionChanged.InvokeAsync(new Selection<TItem>(selectedIndices));
        //    }
        //}
        public void AddAndRemoveKeys(IEnumerable<object> keysToAdd, IEnumerable<int> keysToRemove)
        {
            foreach (var key in keysToAdd)
            {
                if (!selectedKeys.Contains(key))
                    selectedKeys.Add(key);
            }
            foreach (var key in keysToRemove)
            {
                selectedKeys.Remove(key);
            }

            if ((keysToAdd != null && keysToAdd.Count() > 0) || (keysToRemove != null && keysToRemove.Count() > 0))
            {
                doNotRenderOnce = true;
                //selectedIndicesSubject.OnNext(selectedIndices);
                SelectionChanged.InvokeAsync(Selection.SetSelectedKeys(selectedKeys));
            }
        }

        //public void AddAndRemoveIndices(IEnumerable<int> indicesToAdd, IEnumerable<int> indicesToRemove)
        //{
        //    foreach (var index in indicesToAdd)
        //    {
        //        if (!selectedIndices.Contains(index))
        //            selectedIndices.Add(index);
        //    }
        //    foreach (var index in indicesToRemove)
        //    {
        //        selectedIndices.Remove(index);
        //    }

        //    if ((indicesToAdd != null && indicesToAdd.Count() > 0) || (indicesToRemove != null && indicesToRemove.Count() > 0))
        //    {
        //        doNotRenderOnce = true;
        //        //selectedIndicesSubject.OnNext(selectedIndices);
        //        SelectionChanged.InvokeAsync(Selection.SetSelectedIndices(selectedIndices));
        //    }
        //}


        public void ClearSelection()
        {
            if (selectedKeys.Count > 0)
            {
                selectedKeys.Clear();
                doNotRenderOnce = true;
                SelectionChanged.InvokeAsync(Selection.SetSelectedKeys(selectedKeys));
            }
            //if (selectedIndices.Count>0)
            //{
            //    selectedIndices.Clear();
            //    doNotRenderOnce = true;
            //    //selectedIndicesSubject.OnNext(selectedIndices);
            //    SelectionChanged.InvokeAsync(Selection.SetSelectedIndices(selectedIndices));
            //}
        }

        // For end-users to let SelectionMode handle what to do.
        //public void HandleClick(TItem item)
        //{
        //    bool hasChanged = false;
        //    if (SelectionMode == SelectionMode.Multiple)
        //    {
        //        hasChanged = true;
        //        if (selectedIndices.Contains(item))
        //            selectedIndices.Remove(item);
        //        else
        //            selectedIndices.Add(item);
        //    }
        //    else if (SelectionMode == SelectionMode.Single )
        //    {
        //        if (!selectedIndices.Contains(item))
        //        {
        //            hasChanged = true;
        //            selectedIndices.Clear();
        //            selectedIndices.Add(item);
        //        }
        //    }

        //    if (hasChanged)
        //    {
        //        doNotRenderOnce = true;
        //        selectedIndicesSubject.OnNext(selectedIndices);
        //        SelectionChanged.InvokeAsync(new Selection<TItem>(selectedIndices));
        //    }
        //}
        //public void HandleClick(int index)
        //{
        //    bool hasChanged = false;
        //    if (SelectionMode == SelectionMode.Multiple)
        //    {
        //        hasChanged = true;
        //        if (selectedIndices.Contains(index))
        //            selectedIndices.Remove(index);
        //        else
        //            selectedIndices.Add(index);
        //    }
        //    else if (SelectionMode == SelectionMode.Single)
        //    {
        //        if (!selectedIndices.Contains(index))
        //        {
        //            hasChanged = true;
        //            selectedIndices.Clear();
        //            selectedIndices.Add(index);
        //        }
        //    }

        //    if (hasChanged)
        //    {
        //        doNotRenderOnce = true;
        //        //selectedIndicesSubject.OnNext(selectedIndices);
        //        SelectionChanged.InvokeAsync(Selection.SetSelectedIndices(selectedIndices));
        //    }
        //}

        public void HandleClick(object key)
        {
            bool hasChanged = false;
            if (SelectionMode == SelectionMode.Multiple)
            {
                hasChanged = true;
                if (selectedKeys.Contains(key))
                    selectedKeys.Remove(key);
                else
                    selectedKeys.Add(key);
            }
            else if (SelectionMode == SelectionMode.Single)
            {
                if (!selectedKeys.Contains(key))
                {
                    hasChanged = true;
                    selectedKeys.Clear();
                    selectedKeys.Add(key);
                }
            }

            if (hasChanged)
            {
                doNotRenderOnce = true;
                //selectedIndicesSubject.OnNext(selectedIndices);
                SelectionChanged.InvokeAsync(Selection.SetSelectedKeys(selectedKeys));
            }
        }

        // For end-users to let SelectionMode handle what to do.
        //public void HandleToggle(TItem item)
        //{
        //    bool hasChanged = false;
        //    switch (SelectionMode)
        //    {
        //        case SelectionMode.Multiple:
        //            hasChanged = true;
        //            if (selectedIndices.Contains(item))
        //                selectedIndices.Remove(item);
        //            else
        //                selectedIndices.Add(item);
        //            break;
        //        case SelectionMode.Single:
        //            hasChanged = true;
        //            if (selectedIndices.Contains(item))
        //                selectedIndices.Remove(item);
        //            else
        //            {
        //                selectedIndices.Clear();
        //                selectedIndices.Add(item);
        //            }
        //            break;
        //        case SelectionMode.None:
        //            break;
        //    }

        //    if (hasChanged)
        //    {
        //        doNotRenderOnce = true;
        //        selectedIndicesSubject.OnNext(selectedIndices);
        //        SelectionChanged.InvokeAsync(new Selection<TItem>(selectedIndices));
        //    }
        //}
        //public void HandleToggle(int index)
        //{
        //    bool hasChanged = false;
        //    switch (SelectionMode)
        //    {
        //        case SelectionMode.Multiple:
        //            hasChanged = true;
        //            if (selectedIndices.Contains(index))
        //                selectedIndices.Remove(index);
        //            else
        //                selectedIndices.Add(index);
        //            break;
        //        case SelectionMode.Single:
        //            hasChanged = true;
        //            if (selectedIndices.Contains(index))
        //                selectedIndices.Remove(index);
        //            else
        //            {
        //                selectedIndices.Clear();
        //                selectedIndices.Add(index);
        //            }
        //            break;
        //        case SelectionMode.None:
        //            break;
        //    }

        //    if (hasChanged)
        //    {
        //        doNotRenderOnce = true;
        //        //selectedIndicesSubject.OnNext(selectedIndices);
        //        SelectionChanged.InvokeAsync(Selection.SetSelectedIndices(selectedIndices));
        //    }
        //}
        public void HandleToggle(object key)
        {
            bool hasChanged = false;
            switch (SelectionMode)
            {
                case SelectionMode.Multiple:
                    hasChanged = true;
                    if (selectedKeys.Contains(key))
                        selectedKeys.Remove(key);
                    else
                        selectedKeys.Add(key);
                    break;
                case SelectionMode.Single:
                    hasChanged = true;
                    if (selectedKeys.Contains(key))
                        selectedKeys.Remove(key);
                    else
                    {
                        selectedKeys.Clear();
                        selectedKeys.Add(key);
                    }
                    break;
                case SelectionMode.None:
                    break;
            }

            if (hasChanged)
            {
                doNotRenderOnce = true;
                //selectedIndicesSubject.OnNext(selectedIndices);
                SelectionChanged.InvokeAsync(Selection.SetSelectedKeys(selectedKeys));
            }
        }



    }
}
