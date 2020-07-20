using DynamicData;
using DynamicData.Binding;
using DynamicData.Kernel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive.Linq;
using System.Reflection;
using System.Text;

namespace BlazorFluentUI
{
    public static class Statics
    {
        //public static IObservable<IChangeSet<GroupedListItem2<TItem>, AggregationKey>> FlatGroup<TItem, TKey>(this IObservable<IChangeSet<TItem, TKey>> items, IList<Func<TItem, object>> groupBy, int depth, IList<object> groupKeys, IObservable<SortExpressionComparer<TItem>> itemSortExpression)
        //{
        //    if (groupBy != null && groupBy.Count > 0)
        //    {
        //        var firstGroupBy = groupBy.First();
        //        var groups = items.Group(firstGroupBy);
        //        var headerItems = groups.Transform(group =>
        //        {
        //            var tempGroupKeyList = groupKeys.ToList();
        //            tempGroupKeyList.Add(group.Key);
        //            //Debug.WriteLine($"Created Header {group.Key}  with parent: {groupKeys.Last()}");
        //            return new HeaderItem2<TItem>(default, tempGroupKeyList, depth, group.Key.ToString()) as GroupedListItem2<TItem>;
        //        }).ChangeKey((k, x) => new AggregationKey(AggregationType.Header, x.ParentGroupKeys));
        //        var subItems = groups.MergeMany(group =>
        //        {
        //            var tempGroupKeyList = groupKeys.ToList();
        //            tempGroupKeyList.Add(group.Key);
        //            var changeset = group.Cache.Connect().FlatGroup<TItem, TKey>(groupBy.Skip(1).ToList(), depth + 1, tempGroupKeyList, itemSortExpression);
        //            return changeset;
        //        }).ChangeKey((k, x) => new AggregationKey(AggregationType.Item, k));

        //        var flattenedItems = headerItems.Or(subItems);
        //        return flattenedItems;
        //    }
        //    else
        //    {
        //        ////Debug.WriteLine($"Created Item with parent: {string.Join(',', groupKeys.Select(x=>x.ToString()))}");
        //        //if (sortBy != null && sortBy.Count > 0)
        //        //{
        //        //    var count = 0;
        //        //    foreach (var sort in sortBy)
        //        //    {
        //        //        if (sortByDescending[count])
        //        //        {
        //        //            items = items.Sort(SortExpressionComparer<TItem>.Descending(sort.ConvertToIComparable()));
        //        //        }
        //        //        else
        //        //        {
        //        //            items = items.Sort(SortExpressionComparer<TItem>.Descending(sort.ConvertToIComparable()));
        //        //        }
        //        //        count++;
        //        //    }
        //        //}
        //        return items.Sort(itemSortExpression).Transform(item => new PlainItem2<TItem>(item, groupKeys, depth, 0) as GroupedListItem2<TItem>)
        //            .ChangeKey((k, x) => new AggregationKey(AggregationType.Item, k));
        //    }
        //}

        public static IObservable<IChangeSet<GroupedListItem2<TItem>>> FlatGroup<TItem>(this IObservable<IChangeSet<TItem>> items, IList<Func<TItem, object>> groupBy, int depth, IList<object> groupKeys, IObservable<SortExpressionComparer<TItem>> itemSortExpression)
        {
            if (groupBy != null && groupBy.Count > 0)
            {
                var firstGroupBy = groupBy.First();
                var groups = items.GroupOn(firstGroupBy);
                var headerItems = groups.Transform(group =>
                {
                    var tempGroupKeyList = groupKeys.ToList();
                    tempGroupKeyList.Add(group.GroupKey);
                    //Debug.WriteLine($"Created Header {group.Key}  with parent: {groupKeys.Last()}");
                    return new HeaderItem2<TItem>(default, tempGroupKeyList, depth, group.GroupKey.ToString()) as GroupedListItem2<TItem>;
                });
                var subItems = groups.MergeMany(group =>
                {
                    var tempGroupKeyList = groupKeys.ToList();
                    tempGroupKeyList.Add(group.GroupKey);
                    var changeset = group.List.Connect().FlatGroup<TItem>(groupBy.Skip(1).ToList(), depth + 1, tempGroupKeyList, itemSortExpression);
                    return changeset;
                });

                var flattenedItems = headerItems.Or(subItems);
                return flattenedItems;
            }
            else
            {
                ////Debug.WriteLine($"Created Item with parent: {string.Join(',', groupKeys.Select(x=>x.ToString()))}");
                //if (sortBy != null && sortBy.Count > 0)
                //{
                //    var count = 0;
                //    foreach (var sort in sortBy)
                //    {
                //        if (sortByDescending[count])
                //        {
                //            items = items.Sort(SortExpressionComparer<TItem>.Descending(sort.ConvertToIComparable()));
                //        }
                //        else
                //        {
                //            items = items.Sort(SortExpressionComparer<TItem>.Descending(sort.ConvertToIComparable()));
                //        }
                //        count++;
                //    }
                //}
                return items.AutoRefreshOnObservable(x => itemSortExpression)
                        .Sort(itemSortExpression)
                        //.Do(_=>Debug.WriteLine("Comparer changed!"))                        
                        .Transform((item, index) =>
                        {
                            Debug.WriteLine($"Group {string.Join(',',groupKeys.Select(x=>x.ToString()).ToArray())} Index {index}: {System.Text.Json.JsonSerializer.Serialize<TItem>(item)}");
                            return new PlainItem2<TItem>(item, groupKeys, depth, index) as GroupedListItem2<TItem>;
                        }, true);
            }
        }

        public static IObservable<IChangeSet<TDestination, TKey>> TransformWithInlineUpdate<TObject, TKey, TDestination>(this IObservable<IChangeSet<TObject, TKey>> source,
            Func<TObject, TDestination> transformFactory,
            Action<TDestination, TObject> updateAction = null)
        {
            return source.Scan((ChangeAwareCache<TDestination, TKey>)null, (cache, changes) =>
            {
                if (cache == null)
                    cache = new ChangeAwareCache<TDestination, TKey>(changes.Count);

                foreach (var change in changes)
                {
                    switch (change.Reason)
                    {
                        case ChangeReason.Add:
                            cache.AddOrUpdate(transformFactory(change.Current), change.Key);
                            break;
                        case ChangeReason.Update:
                            {
                                if (updateAction == null) continue;

                                var previous = cache.Lookup(change.Key)
                                    .ValueOrThrow(() => new MissingKeyException($"{change.Key} is not found."));

                                updateAction(previous, change.Current);

                                //send a refresh as this will force downstream operators 
                                cache.Refresh(change.Key);
                            }
                            break;
                        case ChangeReason.Remove:
                            cache.Remove(change.Key);
                            break;
                        case ChangeReason.Refresh:
                            cache.Refresh(change.Key);
                            break;
                        case ChangeReason.Moved:
                            //Do nothing !
                            break;
                    }
                }
                return cache;
            }).Select(cache => cache.CaptureChanges());
        }


        public static IObservable<IChangeSet<TItem,object>> Filter<TItem>(this IObservable<IChangeSet<TItem, object>> source, IEnumerable<IObservable<Func<TItem,bool>>> filterPredicates)
        {
            foreach (var filter in filterPredicates)
            {
                source = source.Filter(filter);
            }
            return source;
        }

        public static IObservable<IChangeSet<TItem, object>> Filter<TItem>(this IObservable<IChangeSet<TItem, object>> source, IEnumerable<Func<TItem, bool>> filterPredicates)
        {
            foreach (var filter in filterPredicates)
            {
                source = source.Filter(filter);
            }
            return source;
        }

        public static Func<TItem,IComparable> ConvertToIComparable<TItem>(this Func<TItem,object> func)
        {
            Func<TItem, IComparable> fa = (Func<TItem, IComparable>)Statics.Convert(func, typeof(TItem), typeof(IComparable));
            return fa;
        }
        
        // https://stackoverflow.com/questions/16590685/using-expression-to-cast-funcobject-object-to-funct-tret
        public static Delegate Convert<TItem>(Func<TItem, object> func, Type argType, Type resultType)
        {
            // If we need more versions of func then consider using params Type as we can abstract some of the
            // conversion then.

            Contract.Requires(func != null);
            Contract.Requires(resultType != null);

            var param = Expression.Parameter(argType);
            var convertedParam = new Expression[] { Expression.Convert(param, typeof(TItem)) };

            // This is gnarly... If a func contains a closure, then even though its static, its first
            // param is used to carry the closure, so its as if it is not a static method, so we need
            // to check for that param and call the func with it if it has one...
            Expression call;
            call = Expression.Convert(
                func.Target == null
                ? Expression.Call(func.Method, convertedParam)
                : Expression.Call(Expression.Constant(func.Target), func.Method, convertedParam), resultType);

            var delegateType = typeof(Func<,>).MakeGenericType(argType, resultType);
            return Expression.Lambda(delegateType, call, param).Compile();
        }


        
        
    }
}
