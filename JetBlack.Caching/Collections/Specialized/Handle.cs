using System;
using System.Collections.Generic;

namespace JetBlack.Caching.Collections.Specialized
{
    public struct Handle : IEquatable<Handle>, IEqualityComparer<Handle>
    {
        public readonly long Value;

        public Handle(long value)
            : this()
        {
            Value = value;
        }

        public bool Equals(Handle other)
        {
            return Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            return obj is Handle && Equals((Handle)obj);
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public override string ToString()
        {
            return Value.ToString();
        }

        public bool Equals(Handle x, Handle y)
        {
            return x.Equals(y);
        }

        public int GetHashCode(Handle obj)
        {
            return obj.GetHashCode();
        }

        private static long _next;

        public static Handle Create()
        {
            return new Handle(++_next);
        }
    }
}
