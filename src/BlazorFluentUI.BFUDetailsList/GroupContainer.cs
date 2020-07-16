using DynamicData;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace BlazorFluentUI
{
    public class GroupContainer<TItem> : IDisposable
    {
        public object Key => _group.Key;

        private IGroup<TItem, object, object> _group;

        private ReadOnlyObservableCollection<TItem>? items;
        private ReadOnlyObservableCollection<GroupContainer<TItem>>? groupedItems;

        public ReadOnlyObservableCollection<TItem>? Items => items;
        public ReadOnlyObservableCollection<GroupContainer<TItem>>? GroupedItems => groupedItems;

        IDisposable subscription;

        public GroupContainer(IGroup<TItem,object,object> group, IList<Func<TItem,object>> groupBy)
        {
            _group = group;

            if (groupBy.Count() > 0)
            {
                subscription = group.Cache.Connect()
                   .Group(groupBy.First())
                   .Transform(group => new GroupContainer<TItem>(group, groupBy.Skip(1).ToList()))
                   .Bind(out groupedItems)
                   .Subscribe();
            }
            else
            {
                subscription = group.Cache.Connect()
                   .Bind(out items)
                   .Subscribe();
            }
        }

        public void Dispose()
        {
            subscription?.Dispose();
        }
    }
}
