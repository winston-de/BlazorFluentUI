using System;
using System.Collections.Generic;

namespace BlazorFluentUI
{
    public interface IGroupedListItem<TItem>
    {
        List<GroupedListItem<TItem>> Children { get; set; }
        int Depth { get; set; }
        int Index { get; set; }
        bool IsSelected { get; set; }
        bool IsVisible { get; set; }
        IObservable<bool> IsVisibleObservable { get; }
        TItem Item { get; set; }
        string Key { get; }
        string Name { get; set; }
        HeaderItem<TItem> Parent { get; set; }
        int RecursiveCount { get; }
    }
}