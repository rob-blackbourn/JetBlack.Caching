using System.Collections.Generic;
using System.Linq;

namespace JetBlack.Caching.Collections.Generic
{
    /// <summary>
    /// A circular buffer which implements IList&gt;T&lt;.
    /// </summary>
    /// <typeparam name="T">The type of elements in the buffer.</typeparam>
    public class CircularList<T> : CircularBuffer<T>, IList<T>
    {
        /// <summary>
        /// Creates a circular list of the given capacity.
        /// </summary>
        /// <param name="capacity">The capcity of the list.</param>
        public CircularList(int capacity)
            : base(capacity)
        {
        }

        /// <summary>
        /// Adds an item to the collection. If the collection is full the last item will be discarded.
        /// </summary>
        /// <param name="item">The item to add.</param>
        public void Add(T item)
        {
            Enqueue(item);
        }

        /// <summary>
        /// Determines whether the collection contains a specific value.
        /// </summary>
        /// <param name="item">The item to locate.</param>
        /// <returns>true if item is found otherwise false.</returns>
        public bool Contains(T item)
        {
            return this.Any(x => Equals(x, item));
        }

        /// <summary>
        /// Copies the elements in the collection to an array starting at the given index.
        /// </summary>
        /// <param name="array">The destination array.</param>
        /// <param name="arrayIndex">The zero-based index in array at which copying begins.</param>
        public void CopyTo(T[] array, int arrayIndex)
        {
            foreach (var value in this)
                array[arrayIndex++] = value;
        }

        /// <summary>
        /// Remove an item from the collection.
        /// </summary>
        /// <param name="item">The item to remove.</param>
        /// <returns>If the item was removed true, otherwise false.</returns>
        public bool Remove(T item)
        {
            var index = IndexOf(item);
            if (index == -1) return false;
            RemoveAt(index);
            return true;
        }

        /// <summary>
        /// Always returns true.
        /// </summary>
        public bool IsReadOnly
        {
            get { return false; }
        }
    }
}
