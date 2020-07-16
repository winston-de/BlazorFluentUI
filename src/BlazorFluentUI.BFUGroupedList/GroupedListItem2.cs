using DynamicData;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;

namespace BlazorFluentUI
{
    public class GroupedListItem2<TItem> : IComparable
    {
        private BehaviorSubject<bool> _isVisibleSubject;
        public IObservable<bool> IsVisibleObservable => _isVisibleSubject.AsObservable();
        public bool IsVisible
        {
            get => _isVisibleSubject.Value;
            set
            {
                _isVisibleSubject.OnNext(value);
            }
        }

        private bool _isSelected;
        public bool IsSelected { get => _isSelected; set => _isSelected = value; }

        public TItem Item { get; set; }
        public string Name { get; set; }
        //public int Index { get; set; }
        public int Depth { get; set; }
        //public string Key => GetGroupItemKey(this);
        public System.Collections.Generic.List<GroupedListItem<TItem>> Children { get; set; } = new System.Collections.Generic.List<GroupedListItem<TItem>>();

        public int RecursiveCount => Children.RecursiveCount();



        //private static string GetGroupItemKey(GroupedListItem2<TItem> groupedListItem)
        //{
        //    string key = "";
        //    if (groupedListItem.Parent != null)
        //        key = GetGroupItemKey(groupedListItem.Parent.Key) + "-";
        //    key += groupedListItem.Index;
        //    return key;
        //}

        public List<object> ParentHierarchy { get; set; } = new List<object>();

        public List<object> ParentGroupKeys { get; set; }

        public GroupedListItem2(TItem item, List<object> parentGroupKeys, int depth) 
        {
            _isVisibleSubject = new BehaviorSubject<bool>(true);

            Item = item;
            //Index = index;
            Depth = depth;
            ParentGroupKeys = parentGroupKeys;
            

            //Parent?.IsOpenObservable.CombineLatest(Parent.IsVisibleObservable, (open, visible) => !visible ? false : (open ? true : false)).Subscribe(shouldBeVisible =>
            //{
            //    IsVisible = shouldBeVisible;
            //});
        }

        //public int Compare(object x, object y)
        //{
        //    if (x is GroupedListItem2<TItem> && y is GroupedListItem2<TItem>)
        //    {
        //        var a = (GroupedListItem2<TItem>)x;
        //        var b = (GroupedListItem2<TItem>)y;
        //        //if (a.Depth == 0 && b.Depth == 0) //both root groups
        //        //{
        //        //    return a.Name.CompareTo(b.Name);
        //        //}
        //        //else 
        //        if (a.ParentGroupKeys.Count == 0 && b.ParentGroupKeys.Count == 0)
        //        {
        //            return a.Name.CompareTo(b.Name);
        //        }
        //        if (a.ParentGroupKeys.Count > b.ParentGroupKeys.Count)
        //        {
        //            var result = a.ParentGroupKeys[b.ParentGroupKeys.Count - 1].ToString().CompareTo(b.ParentGroupKeys[b.ParentGroupKeys.Count - 1].ToString());
        //            if (result == 0)
        //                return 1;
        //            else
        //                return result;
        //        }
        //        else if (a.ParentGroupKeys.Count < b.ParentGroupKeys.Count)
        //        {
        //            var result = a.ParentGroupKeys[a.ParentGroupKeys.Count - 1].ToString().CompareTo(a.ParentGroupKeys[a.ParentGroupKeys.Count - 1].ToString());
        //            if (result == 0)
        //                return -1;
        //            else
        //                return result;
        //        }
        //        else
        //        {
        //            //compare each key starting from first
        //            for (var i = 0; i < a.ParentGroupKeys.Count; i++)
        //            {
        //                var result = a.ParentGroupKeys[i].ToString().CompareTo(a.ParentGroupKeys[i].ToString());
        //                if (result != 0)
        //                    return result;
        //            }
        //            // if here, then groups all matched.
        //            return 0;
        //        }
        //    }
        //    else
        //        return 0; //not groupedItem
        //}

        public int CompareTo(object obj)
        {
            if (obj is GroupedListItem2<TItem> )
            {
                var b = (GroupedListItem2<TItem>)obj;
                //if (a.Depth == 0 && b.Depth == 0) //both root groups
                //{
                //    return a.Name.CompareTo(b.Name);
                //}
                //else 
                if (this.ParentGroupKeys.Count > b.ParentGroupKeys.Count)
                {
                    //if (b.ParentGroupKeys.Count == 0)
                    //    return this.ParentGroupKeys[0].ToString().CompareTo(b.Name);
                    var result = this.ParentGroupKeys[b.ParentGroupKeys.Count - 1].ToString().CompareTo(b.ParentGroupKeys[b.ParentGroupKeys.Count - 1].ToString());
                    if (this is HeaderItem2<TItem> && b is PlainItem2<TItem>)
                        Debug.WriteLine($"header {string.Join(',', this.ParentGroupKeys.Select(x => x.ToString()))} was {(result > 0 ? "after" : result < 0 ? "before" : "equal to")} item {string.Join(',', b.ParentGroupKeys.Select(x => x.ToString()))}");
                    else if (this is PlainItem2<TItem> && b is HeaderItem2<TItem>)
                        Debug.WriteLine($"item {string.Join(',', this.ParentGroupKeys.Select(x => x.ToString()))} was {(result > 0 ? "after" : result < 0 ? "before" : "equal to")} header {string.Join(',', b.ParentGroupKeys.Select(x => x.ToString()))}");
                    else if (this is HeaderItem2<TItem> && b is HeaderItem2<TItem>)
                        Debug.WriteLine($"header {string.Join(',', this.ParentGroupKeys.Select(x => x.ToString()))} was {(result > 0 ? "after" : result < 0 ? "before" : "equal to")} header {string.Join(',', b.ParentGroupKeys.Select(x => x.ToString()))}");
                    if (result == 0)
                        return 1;
                    else
                        return result;
                }
                else if (this.ParentGroupKeys.Count < b.ParentGroupKeys.Count)
                {
                    //if (this.ParentGroupKeys.Count == 0)
                    //    return this.Name.CompareTo(b.ParentGroupKeys[0]);
                    var result = this.ParentGroupKeys[this.ParentGroupKeys.Count - 1].ToString().CompareTo(b.ParentGroupKeys[this.ParentGroupKeys.Count - 1].ToString());
                    if (this is HeaderItem2<TItem> && b is PlainItem2<TItem>)
                        Debug.WriteLine($"header {string.Join(',', this.ParentGroupKeys.Select(x => x.ToString()))} was {(result > 0 ? "after" : result < 0 ? "before" : "equal to")} item {string.Join(',', b.ParentGroupKeys.Select(x => x.ToString()))}");
                    else if (this is PlainItem2<TItem> && b is HeaderItem2<TItem>)
                        Debug.WriteLine($"item {string.Join(',', this.ParentGroupKeys.Select(x => x.ToString()))} was {(result > 0 ? "after" : result < 0 ? "before" : "equal to")} header {string.Join(',', b.ParentGroupKeys.Select(x => x.ToString()))}");
                    else if (this is HeaderItem2<TItem> && b is HeaderItem2<TItem>)
                        Debug.WriteLine($"header {string.Join(',', this.ParentGroupKeys.Select(x => x.ToString()))} was {(result > 0 ? "after" : result < 0 ? "before" : "equal to")} header {string.Join(',', b.ParentGroupKeys.Select(x => x.ToString()))}");
                    if (result == 0)
                        return -1;
                    else
                        return result;
                }
                else
                {
                    //compare each key starting from first
                    for (var i = 0; i < this.ParentGroupKeys.Count; i++)
                    {
                        var result = this.ParentGroupKeys[i].ToString().CompareTo(b.ParentGroupKeys[i].ToString());
                        if (result != 0)
                        {
                            //if (this is HeaderItem2<TItem> && b is HeaderItem2<TItem>)
                            //    Debug.WriteLine($"{this.Name} was {(result > 0 ? "after" : "before")} {b.Name}");
                            //if (this is PlainItem2<TItem> && b is PlainItem2<TItem>)
                            //    Debug.WriteLine($"{string.Join(',', this.ParentGroupKeys.Select(x => x.ToString()))} was {(result > 0 ? "after" : "before")} {string.Join(',', b.ParentGroupKeys.Select(x => x.ToString()))}");
                            return result;
                        }
                    }
                    // if here, then groups all matched.
                    return 0;
                }
            }
            else
                return 0; //not groupedItem
        }
    }

    public class HeaderItem2<TItem> : GroupedListItem2<TItem>
    {
        public bool IsOpen
        {
            get => isOpenSubject.Value;
            set
            {
                isOpenSubject.OnNext(value);
            }
        }

        private BehaviorSubject<bool> isOpenSubject;
        public IObservable<bool> IsOpenObservable => isOpenSubject.AsObservable();


        public HeaderItem2(TItem item, List<object> parentGroupKeys, int depth, string name)
            : base(item, parentGroupKeys, depth)
        {
            isOpenSubject = new BehaviorSubject<bool>(true);
            Name = name;
        }

    }

    public class PlainItem2<TItem> : GroupedListItem2<TItem>
    {
        public PlainItem2(TItem item, List<object> parentGroupKeys, int depth)
            : base(item, parentGroupKeys, depth)
        {

        }
    }
}
