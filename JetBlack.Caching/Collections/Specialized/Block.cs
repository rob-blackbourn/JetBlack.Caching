using System;
using System.Collections.Generic;

namespace JetBlack.Caching.Collections.Specialized
{
    public struct Block : IEquatable<Block>, IEqualityComparer<Block>
    {
        public Handle Handle { get; private set; }
        public long Index { get; private set; }
        public long Length { get; private set; }

        public Block(long index, long length)
            : this()
        {
            Handle = Handle.Create();
            Index = index;
            Length = length;
        }

        public bool Equals(Block other)
        {
            return Handle.Equals(other.Handle);
        }

        public override bool Equals(object obj)
        {
            return obj is Block && Equals((Block)obj);
        }

        public override int GetHashCode()
        {
            return Handle.GetHashCode();
        }

        public override string ToString()
        {
            return string.Format("[Block: Handle={0}, Index={1}, Length={2}]", Handle, Index, Length);
        }

        public bool Equals(Block x, Block y)
        {
            return x.Equals(y);
        }

        public int GetHashCode(Block obj)
        {
            return obj.GetHashCode();
        }
    }
}
