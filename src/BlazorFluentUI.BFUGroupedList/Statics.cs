using DynamicData;
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
        public static IObservable<IChangeSet<GroupedListItem2<TItem>, object>> FlatGroup<TItem>(this IObservable<IChangeSet<TItem, object>> items, IList<Func<TItem, object>> groupBy, int depth, List<object> groupKeys)
        {
            if (groupBy != null && groupBy.Count > 0)
            {
                var firstGroupBy = groupBy.First();
                var groups = items.Group(firstGroupBy);
                var headerItems = groups.Transform(group =>
                {
                    var tempGroupKeyList = groupKeys.ToList();
                    tempGroupKeyList.Add(group.Key);
                    //Debug.WriteLine($"Created Header {group.Key}  with parent: {groupKeys.Last()}");
                    return new HeaderItem2<TItem>(default, tempGroupKeyList, depth, group.Key.ToString()) as GroupedListItem2<TItem>;
                });
                var subItems = groups.MergeMany(group =>
                {
                    var tempGroupKeyList = groupKeys.ToList();
                    tempGroupKeyList.Add(group.Key);
                    var changeset = group.Cache.Connect().FlatGroup(groupBy.Skip(1).ToList(), depth + 1, tempGroupKeyList);
                    return changeset;
                });

                var flattenedItems = headerItems.Or(subItems);
                return flattenedItems;
            }
            else
            {
                //Debug.WriteLine($"Created Item with parent: {string.Join(',', groupKeys.Select(x=>x.ToString()))}");
                return items.Transform(item => new PlainItem2<TItem>(item, groupKeys, depth) as GroupedListItem2<TItem>);
            }
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
