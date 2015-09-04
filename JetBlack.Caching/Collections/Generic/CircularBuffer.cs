using System;
using System.Collections;
using System.Collections.Generic;

namespace JetBlack.Caching.Collections.Generic
{
    /// <summary>
    /// A circular buffer is buffer of fixed length. When the buffer is full, subsequent
    /// writes wrap, overwriting previous values. It is useful when you are only
    /// interested in the most recent values.
    /// </summary>
    /// <typeparam name="T">The type of elements in the buffer.</typeparam>
    public class CircularBuffer<T> : ICircularBuffer<T>, IEnumerable<T>
    {
        private T[] _buffer;
        private int _head;
        private int _tail;

        /// <summary>
        /// Create a circular buffer with the specified capacity.
        /// </summary>
        /// <param name="capacity">The maximum number of items the buffer will hold.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the capacity is negative.</exception>
        public CircularBuffer(int capacity)
        {
            if (capacity < 0)
                throw new ArgumentOutOfRangeException("capacity", "must be positive");
            _buffer = new T[capacity];
            _head = capacity - 1;
        }

        /// <summary>
        /// The number of items in the buffer.
        /// </summary>
        public int Count { get; private set; }

        /// <summary>
        /// The maximum number of items the buffer will hold.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Thrown in the value is negative.</exception>
        public int Capacity
        {
            get { return _buffer.Length; }
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException("value", "must be positive");

                if (value == _buffer.Length)
                    return;

                var buffer = new T[value];
                var count = 0;
                while (Count > 0 && count < value)
                    buffer[count++] = Dequeue();

                _buffer = buffer;
                Count = count;
                _head = count - 1;
                _tail = 0;
            }
        }

        /// <summary>
        /// Add an item to the buffer. If the buffer is already full, the last item will be discarded.
        /// </summary>
        /// <param name="item">The item to enqueue.</param>
        /// <returns>The item that was overwritten or <code>default(T)</code>.</returns>
        public T Enqueue(T item)
        {
            _head = (_head + 1) % Capacity;
            var overwritten = _buffer[_head];
            _buffer[_head] = item;
            if (Count == Capacity)
                _tail = (_tail + 1) % Capacity;
            else
                ++Count;
            return overwritten;
        }

        /// <summary>
        /// Takes an item off the end of the buffer.
        /// </summary>
        /// <returns>The last item in the buffer.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the queue is empty.</exception>
        public T Dequeue()
        {
            if (Count == 0)
                throw new InvalidOperationException("queue exhausted");

            var dequeued = _buffer[_tail];
            _buffer[_tail] = default(T);
            _tail = (_tail + 1) % Capacity;
            --Count;
            return dequeued;
        }

        /// <summary>
        /// Removes all items from the buffer.
        /// </summary>
        /// <remarks>
        /// Note that the items are not actuall removed, so will not be garbage collected. To achieve this you must dequeue each item.
        /// </remarks>
        public void Clear()
        {
            _head = Capacity - 1;
            _tail = 0;
            Count = 0;
        }

        /// <summary>
        /// The indexer into the buffer.
        /// </summary>
        /// <param name="index">The point at which to get or set the buffer.</param>
        /// <returns>The item at the given index.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the index is outside the bounds of the buffer.</exception>
        public T this[int index]
        {
            get
            {
                if (index < 0 || index >= Count)
                    throw new ArgumentOutOfRangeException("index");

                return Get(index);
            }
            set
            {
                if (index < 0 || index >= Count)
                    throw new ArgumentOutOfRangeException("index");

                Set(index, value);
            }
        }

        /// <summary>
        /// Finds the index of the given item in the buffer.
        /// </summary>
        /// <param name="item">The item to find.</param>
        /// <returns>The index of the specified item or -1.</returns>
        public int IndexOf(T item)
        {
            for (var i = 0; i < Count; ++i)
                if (Equals(item, Get(i)))
                    return i;
            return -1;
        }

        /// <summary>
        /// Insert an item into the buffer at the given point.
        /// </summary>
        /// <param name="index">The point at which to insert the item.</param>
        /// <param name="item">The item to be inserted.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the index is outside the range of the buffer.</exception>
        public void Insert(int index, T item)
        {
            if (index < 0 || index > Count)
                throw new ArgumentOutOfRangeException("index");

            if (Count == index)
                Enqueue(item);
            else
            {
                var last = this[Count - 1];
                for (var i = index; i < Count - 2; ++i)
                    Set(i + 1, Get(i));
                Set(index, item);
                Enqueue(last);
            }
        }

        /// <summary>
        /// Remove the item at the specific point.
        /// </summary>
        /// <param name="index">The point at which the item should be removed</param>
        /// <remarks>
        /// Note that the item removed is not returned. This would require extra work, and may often not be required.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the index is outside the range of the buffer.</exception>
        public void RemoveAt(int index)
        {
            if (index < 0 || index >= Count)
                throw new ArgumentOutOfRangeException("index");

            for (var i = index; i > 0; --i)
                Set(i, Get(i - 1));
            Dequeue();
        }

        /// <summary>
        /// Gets an enumerator for the buffer.
        /// </summary>
        /// <returns>An numerator.</returns>
        public IEnumerator<T> GetEnumerator()
        {
            if (Count == 0 || Capacity == 0)
                yield break;

            for (var i = 0; i < Count; ++i)
                yield return Get(i);
        }

        /// <summary>
        /// Gets an untyped enumerator for the buffer.
        /// </summary>
        /// <returns>An untyped enumerator.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Gets the value at a given point without bounds checking.
        /// </summary>
        /// <param name="index">The point at which to return the value.</param>
        /// <returns>The value at the given point.</returns>
        private T Get(int index)
        {
            return _buffer[(_tail + index) % Capacity];
        }

        /// <summary>
        /// Sets a value in the buffer without bounds checking.
        /// </summary>
        /// <param name="index">The point at which to set the value.</param>
        /// <param name="value">The value to set at the given point.</param>
        private void Set(int index, T value)
        {
            _buffer[(_tail + index) % Capacity] = value;
        }

        /// <summary>
        /// Provides a string representation of the buffer.
        /// </summary>
        /// <returns>A string representation of the buffer.</returns>
        public override string ToString()
        {
            return string.Format("Capacity={0}, Count={1}, Buffer=[{2}]", Capacity, Count, string.Join(",", this));
        }
    }
}
