using System;
using System.Collections.Generic;
using System.Text;

namespace BlazorFluentUI
{
    public struct AggregationKey
    {
        public AggregationType Type { get; }
        public object Value { get; }

        public AggregationKey(AggregationType type, object value)
        {
            Type = type;
            Value = value;
        }

        #region Equality

        public bool Equals(AggregationKey other)
        {
            return Type == other.Type && object.Equals(Value, other.Value);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is AggregationKey && Equals((AggregationKey)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((int)Type * 397) ^ (Value != null ? Value.GetHashCode() : 0);
            }
        }

        #endregion

        public override string ToString()
        {
            return $"{Type} ({Value})";
        }
    }

    public enum AggregationType
    {
        Item,
        Header
    }
}
