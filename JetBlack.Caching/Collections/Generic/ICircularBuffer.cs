namespace JetBlack.Caching.Collections.Generic
{
    /// <summary>
    /// The interface for a circular buffer.
    /// </summary>
    /// <typeparam name="T">The type of items managed by the buffer.</typeparam>
    public interface ICircularBuffer<T>
    {
        /// <summary>
        /// The number of items in the buffer.
        /// </summary>
        int Count { get; }

        /// <summary>
        /// The maximum number of items the buffer will hold.
        /// </summary>
        int Capacity { get; set; }

        /// <summary>
        /// Add an item to the buffer. If the buffer is already full, the last item will be discarded.
        /// </summary>
        /// <param name="item">The item to enqueue.</param>
        /// <returns>The item that was overwritten or <code>default(T)</code>.</returns>
        T Enqueue(T item);

        /// <summary>
        /// Takes an item off the end of the buffer.
        /// </summary>
        /// <returns>The last item in the buffer.</returns>
        T Dequeue();

        /// <summary>
        /// Removes all items from the buffer.
        /// </summary>
        void Clear();

        /// <summary>
        /// The indexer into the buffer.
        /// </summary>
        /// <param name="index">The point at which to get or set the buffer.</param>
        /// <returns>The item at the given index.</returns>
        T this[int index] { get; set; }

        /// <summary>
        /// Finds the index of the given item in the buffer.
        /// </summary>
        /// <param name="item">The item to find.</param>
        /// <returns>The index of the specified item or -1.</returns>
        int IndexOf(T item);

        /// <summary>
        /// Insert an item into the buffer at the given point.
        /// </summary>
        /// <param name="index">The point at which to insert the item.</param>
        /// <param name="item">The item to be inserted.</param>
        void Insert(int index, T item);

        /// <summary>
        /// Remove the item at the specific point.
        /// </summary>
        /// <param name="index">The point at which the item should be removed</param>
        void RemoveAt(int index);
    }
}
