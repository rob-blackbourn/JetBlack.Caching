using System.Collections.Generic;
using System.Linq;

namespace JetBlack.Caching.Collections.Generic
{
    public class CircularList<T> : CircularBuffer<T>, IList<T>
    {
        public CircularList(int capacity)
            : base(capacity)
        {
        }

        public void Add(T item)
        {
            Enqueue(item);
        }

        public bool Contains(T item)
        {
            return this.Any(x => Equals(x, item));
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            foreach (var value in this)
                array[arrayIndex++] = value;
        }

        public bool Remove(T item)
        {
            var index = IndexOf(item);
            if (index == -1) return false;
            RemoveAt(index);
            return true;
        }

        public bool IsReadOnly
        {
            get { return false; }
        }
    }
}
