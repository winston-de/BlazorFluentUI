using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace BlazorFluentUI
{
    public static class Statics
    {
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
