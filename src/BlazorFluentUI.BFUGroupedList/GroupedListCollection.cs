using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace BlazorFluentUI
{
    /// <summary>
    /// Workaround for Blazor compiler not being able to use nested generics 3 deep
    /// </summary>
    public class GroupedListCollection<TItem>
    {
        public System.Collections.ObjectModel.ReadOnlyObservableCollection<GroupedListItem<TItem>> GroupedListItems { get; set; }
    }
}
