# JetBlack.Caching

This project contains code I found useful for caching problems. It includes circular buffers, timout dictionaries, and persistent dictionaries.

## Circular Buffer in C#

A circular buffer is buffer of fixed length. When the buffer is full, subsequent writes wrap, overwriting previous values. It is useful when you are only interested in the most recent values.

### Design Goal

This is a circular buffer I needed as a component for a caching layer. It is largely a blatant ripoff of the many implementations previously published on the web, with a few changes which might prove useful to those with similar objectives.

My first specific requirement was to model the structure as a queue, so the primary interaction is Enqueue and Dequeue. As my caching layer has an in memory cache and a persistent cache, I needed the Enqueue to return the overwritten value (if there was one). Lastly I needed to be able to arbitrarily move things around in the queue, so I could control the order and contents.

### The Interface

All the obvious candidates are in the interface. You can see the queue style interaction. Also note the enqueue returns the overwritten value if one exists (otherwise it will be default(T)). The indexer methods, IndexOf, InsertAt, and RemoveAt provide the mechanism to manipulate the queue directly.

I could have provided item lookups by value rather than index, but these would have still required the indexing operators and I wanted to keep the class small. It should be clear how a derived class, (possibly implementing IList<T>) could be simply implemented.

```cs
using System.Collections.Generic;

namespace JetBlack.Caching.Collections.Generic
{
	public interface ICircularBuffer<T>
	{
		int Count { get; }
		int Capacity { get; set; }
		T Enqueue (T item);
		T Dequeue();
		void Clear();
		T this [int index] { get; set; }
		int IndexOf (T item);
		void Insert(int index, T item);
		void RemoveAt (int index);
	}
}
```

### The Implementation

The code follows the traditional circular buffer pattern of declaring a fixed length array, then maintaining an index to the head and tail of the array. Typically the size of the buffer is defined by the constructor, but I have included a Capacity property, to allow more sympathetic subclassing.

Though not strictley necessary it seemed convenient to implement IEnumerable<T>. The amount of code required is small, and it provides Linq compatibility at little extra cost.

```cs
using System;
using System.Collections;
using System.Collections.Generic;
namespace JetBlack.Caching.Collections.Generic
{
	public class CircularBuffer<T> : ICircularBuffer<T>, IEnumerable<T>

	{
		private T[] _buffer;
		private int _head;
		private int _tail;

		public CircularBuffer(int capacity)
		{
			if (capacity < 0)
				throw new ArgumentOutOfRangeException ("capacity", "must be positive");
			_buffer = new T[capacity];
			_head = capacity - 1;
		}

		public int Count { get; private set; }

		public int Capacity {
                        get { return _buffer.Length; }
			set
			{
				if (value < 0)
					throw new ArgumentOutOfRangeException ("value", "must be positive");

                                if (value == _buffer.Length)
					return;

				var buffer = new T[value];
				var count = 0;
				while (Count > 0 && count < value)
					buffer [count++] = Dequeue ();

				_buffer = buffer;
				Count = count;
				_head = count - 1;
				_tail = 0;
			}
		}

		public T Enqueue (T item)
		{
			_head = (_head + 1) % Capacity;
			var overwritten = _buffer [_head];
			_buffer [_head] = item;
			if (Count == Capacity)
				_tail = (_tail + 1) % Capacity;
			else
				++Count;
			return overwritten;
		}

		public T Dequeue ()
		{
			if (Count == 0)
				throw new InvalidOperationException ("queue exhausted");

			var dequeued = _buffer [_tail];
			_buffer [_tail] = default(T);
			_tail = (_tail + 1) % Capacity;
			--Count;
			return dequeued;
		}

		public void Clear ()
		{
			_head = Capacity - 1;
			_tail = 0;
			Count = 0;
		}

		public T this [int index] {
			get {
				if (index < 0 || index >= Count)
					throw new ArgumentOutOfRangeException ("index");

				return _buffer [(_tail + index) % Capacity];
			}
			set {
				if (index < 0 || index >= Count)
					throw new ArgumentOutOfRangeException ("index");

				_buffer [(_tail + index) % Capacity] = value;
			}
		}

		public int IndexOf (T item)
		{
			for (var i = 0; i < Count; ++i)
				if (Equals (item, this [i]))
					return i;
			return -1;
		}

		public void Insert (int index, T item)
		{
			if (index < 0 || index > Count)
				throw new ArgumentOutOfRangeException ("index");

			if (Count == index)
				Enqueue (item);
			else
			{
				var last = this [Count - 1];
				for (var i = index; i < Count - 2; ++i)
					this [i + 1] = this [i];
				this [index] = item;
				Enqueue (last);
			}
		}

		public void RemoveAt (int index)
		{
			if (index < 0 || index >= Count)
				throw new ArgumentOutOfRangeException ("index");

			for (var i = index; i > 0; --i)
				this [i] = this [i - 1];
			Dequeue ();
		}

		public IEnumerator<T> GetEnumerator ()
		{
			if (Count == 0 || Capacity == 0)
				yield break;

			for (var i = 0; i < Count; ++i)
				yield return this [i];
		}

		IEnumerator IEnumerable.GetEnumerator ()
		{
			return GetEnumerator ();
		}
	}
}
```

### Tests
Here are some simple tests which also demonstrate how to use the class.

```cs
using System;
using JetBlack.Core.Collections.Generic;
using NUnit.Framework;

namespace JetBlack.Caching.Test.Collections.Generic
{
	[TestFixture]
	public class CircularBufferFixture
	{
		[Test]
		public void TestOverwrite()
		{
			var buffer = new CircularBuffer<long>(3);
			Assert.AreEqual(default(long), buffer.Enqueue(1));
			Assert.AreEqual(default(long), buffer.Enqueue(2));
			Assert.AreEqual(default(long), buffer.Enqueue(3));
			Assert.AreEqual(1, buffer.Enqueue(4));
			Assert.AreEqual(3, buffer.Count);
			Assert.AreEqual(2, buffer.Dequeue());
			Assert.AreEqual(3, buffer.Dequeue());
			Assert.AreEqual(4, buffer.Dequeue());
			Assert.AreEqual(0, buffer.Count);
		}

		[Test]
		public void TestUnderwrite()
		{
			var buffer = new CircularBuffer<long>(5);
			Assert.AreEqual(default(long), buffer.Enqueue(1));
			Assert.AreEqual(default(long), buffer.Enqueue(2));
			Assert.AreEqual(default(long), buffer.Enqueue(3));
			Assert.AreEqual(3, buffer.Count);
			Assert.AreEqual(1, buffer.Dequeue());
			Assert.AreEqual(2, buffer.Dequeue());
			Assert.AreEqual(3, buffer.Dequeue());
			Assert.AreEqual(0, buffer.Count);
		}

		[Test]
		public void TestIncreaseCapacityWhenFull()
		{
			var buffer = new CircularBuffer<long>(3);
			Assert.AreEqual(default(long), buffer.Enqueue(1));
			Assert.AreEqual(default(long), buffer.Enqueue(2));
			Assert.AreEqual(default(long), buffer.Enqueue(3));
			Assert.AreEqual(3, buffer.Count);
			buffer.Capacity = 4;
			Assert.AreEqual(3, buffer.Count);
			Assert.AreEqual(1, buffer.Dequeue());
			Assert.AreEqual(2, buffer.Dequeue());
			Assert.AreEqual(3, buffer.Dequeue());
			Assert.AreEqual(0, buffer.Count);
		}

		[Test]
		public void TestDecreaseCapacityWhenFull()
		{
			var buffer = new CircularBuffer<long>(3);
			Assert.AreEqual(default(long), buffer.Enqueue(1));
			Assert.AreEqual(default(long), buffer.Enqueue(2));
			Assert.AreEqual(default(long), buffer.Enqueue(3));
			Assert.AreEqual(3, buffer.Count);
			buffer.Capacity = 2;
			Assert.AreEqual(2, buffer.Count);
			Assert.AreEqual(1, buffer.Dequeue());
			Assert.AreEqual(2, buffer.Dequeue());
			Assert.AreEqual(0, buffer.Count);
		}

		[Test]
		public void TestEnumerationWhenFull()
		{
			var buffer = new CircularBuffer<long>(3);
			Assert.AreEqual(default(long), buffer.Enqueue(1));
			Assert.AreEqual(default(long), buffer.Enqueue(2));
			Assert.AreEqual(default(long), buffer.Enqueue(3));
			var i = 0;
			foreach (var value in buffer)
				Assert.AreEqual(++i, value);
			Assert.AreEqual(i, 3);
		}

		[Test]
		public void TestEnumerationWhenPartiallyFull()
		{
			var buffer = new CircularBuffer<long>(3);
			Assert.AreEqual(default(long), buffer.Enqueue(1));
			Assert.AreEqual(default(long), buffer.Enqueue(2));
			var i = 0;
			foreach (var value in buffer)
				Assert.AreEqual(++i, value);
			Assert.AreEqual(i, 2);
		}

		[Test]
		public void TestEnumerationWhenEmpty()
		{
			var buffer = new CircularBuffer<long>(3);
			foreach (var value in buffer)
				Assert.Fail("Unexpected Value: " + value);
		}

		[Test]
		public void TestRemoveAt()
		{
			var buffer = new CircularBuffer<long>(5);
			Assert.AreEqual(default(long), buffer.Enqueue(1));
			Assert.AreEqual(default(long), buffer.Enqueue(2));
			Assert.AreEqual(default(long), buffer.Enqueue(3));
			Assert.AreEqual(default(long), buffer.Enqueue(4));
			Assert.AreEqual(default(long), buffer.Enqueue(5));
			buffer.RemoveAt(buffer.IndexOf(2));
			buffer.RemoveAt(buffer.IndexOf(4));
			Assert.AreEqual(3, buffer.Count);
			Assert.AreEqual(1, buffer.Dequeue());
			Assert.AreEqual(3, buffer.Dequeue());
			Assert.AreEqual(5, buffer.Dequeue());
			Assert.AreEqual(0, buffer.Count);
			Assert.AreEqual(default(long), buffer.Enqueue(1));
			Assert.AreEqual(default(long), buffer.Enqueue(2));
			Assert.AreEqual(default(long), buffer.Enqueue(3));
			Assert.AreEqual(default(long), buffer.Enqueue(4));
			Assert.AreEqual(default(long), buffer.Enqueue(5));
			buffer.RemoveAt(buffer.IndexOf(1));
			buffer.RemoveAt(buffer.IndexOf(3));
			buffer.RemoveAt(buffer.IndexOf(5));
			Assert.AreEqual(2, buffer.Count);
			Assert.AreEqual(2, buffer.Dequeue());
			Assert.AreEqual(4, buffer.Dequeue());
			Assert.AreEqual(0, buffer.Count);

		}
	}
}
```