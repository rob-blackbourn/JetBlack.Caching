# JetBlack.Caching

This project contains code I found useful for caching problems. It includes
circular buffers, timout dictionaries, and persistent dictionaries.

## Circular Buffer

A circular buffer is buffer of fixed length. When the buffer is full, subsequent
writes wrap, overwriting previous values. It is useful when you are only
interested in the most recent values.

### Usage Example

To whet your appetite, here are some examples.

```cs
// Create a buffer with a capacity of 5 items.
var buffer = new CircularBuffer<long>(5);

// Add three.
foreach (var i in Enumerable.Range(1, 3))
    buffer.Enqueue(i);
Debug.WriteLine(buffer);
// Capacity=5, Count=3, Buffer=[1,2,3]

// Add three more.
foreach (var i in Enumerable.Range(4, 3))
    buffer.Enqueue(i);
Debug.WriteLine(buffer);
// Capacity=5, Count=5, Buffer=[2,3,4,5,6]

// Remove the third.
var value = buffer.RemoveAt(3);
Debug.WriteLine(buffer);
// Capacity=5, Count=4, Buffer=[2,3,4,6]

// Re-insert it.
buffer.Insert(3, value);
Debug.WriteLine(buffer);
// Capacity=5, Count=5, Buffer=[2,3,4,5,6]

// Dequeue.
Debug.Print("Value = {0}", buffer.Dequeue());
// Value = 2
Debug.WriteLine(buffer);
// Capacity=5, Count=4, Buffer=[3,4,5,6]

// Increase the capacity.
buffer.Capacity = 6;
Debug.WriteLine(buffer);
// Capacity=6, Count=4, Buffer=[3,4,5,6]

// Add three more.
foreach (var i in Enumerable.Range(7, 3))
    buffer.Enqueue(i);
Debug.WriteLine(buffer);
// Capacity=6, Count=6, Buffer=[4,5,6,7,8,9]

// Reduce the capacity.
buffer.Capacity = 4;
Debug.WriteLine(buffer);
// Capacity=4, Count=4, Buffer=[4,5,6,7]

// Clear the buffer.
buffer.Clear();
Debug.WriteLine(buffer);
// Capacity=4, Count=0, Buffer=[]
```

### Design Goal

This is a circular buffer I needed as a component for a caching layer. It is
largely a blatant ripoff of the many implementations previously published on
the web, with a few changes which might prove useful to those with similar
objectives.

My first specific requirement was to model the structure as a queue, so the
primary interaction is `Enqueue` and `Dequeue`. As my caching layer has an in
memory cache and a persistent cache, I needed the `Enqueue` to return the
overwritten value (if there was one). Lastly I needed to be able to arbitrarily
move things around in the queue, so I could control the order and contents.

### The Interface

All the obvious candidates are in the interface. You can see the queue style
interaction. Also note the enqueue returns the overwritten value if one exists
(otherwise it will be `default(T)`). The indexer methods, `IndexOf`,
`InsertAt`, and `RemoveAt` provide the mechanism to manipulate the queue
directly.

I could have provided item lookups by value rather than index, but these would
have still required the indexing operators and I wanted to keep the class small.
It should be clear how a derived class, (possibly implementing `IList<T>`)
could be trivially implemented.

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

The code follows the traditional circular buffer pattern of declaring a fixed
length array, then maintaining an index to the head and tail of the array.
Typically the size of the buffer is defined by the constructor, but I have
included a Capacity property, to allow more sympathetic subclassing.

Though not strictley necessary it seemed convenient to implement `IEnumerable<T>`.
The amount of code required is small, and it provides Linq compatibility at
little extra cost.

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
			get
          {
				if (index < 0 || index >= Count)
					throw new ArgumentOutOfRangeException ("index");

				return _buffer [(_tail + index) % Capacity];
			}
			set
          {
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

## Heap

This section describes a heap data structure. In this implementation a heap
manages a sequential array of data which grows upwards from the bottom.
Blocks of this array can be allocated, freed, read and written to through
handles. The heap attempts to keep itself small by managing a list of free
blocks that can be reallocated.

### Design

The primary operations handled by the heap will be memory management: `Allocate`
and `Deallocate`, and reading and writing: `Read` and `Write`. As will become
clear later on it is also useful to be able to discover allocations, so one
less obvious operation `GetAllocatedBlock` is included.

Because the heap manages its data internally, we need some utility classes. First
a `Handle` through which we can refer to a block that has been allocated. Second
we define the `Block` which defines the area in the heap to which the `Handle`
refers.

#### Handle

The handle is irritatingly large for such a trivial data structure. This is
because it implements a couple of interfaces for equality, and a factory class
for generating new handles.

```cs
using System;
using System.Collections.Generic;

namespace JetBlack.Caching.Collections.Specialized
{
    public struct Handle : IEquatable<Handle>, IEqualityComparer<Handle>
    {
        public readonly long Value;

        public Handle(long value) : this()
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
```

#### Block

The `Block` is rather more terse. It has the index offset into the heap, and a
length. Both the handle and the block are immutable.

```cs
using System;
using System.Collections.Generic;

namespace JetBlack.Patterns
{
    public struct Block : IEquatable<Block>, IEqualityComparer<Block>
    {
        public Handle Handle { get; private set; }
        public long Index { get; private set; }
        public long Length { get; private set; }

        public Block(long index, long length) : this()
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
```

### The Heap Interface

Now we have our basic data structures we can define the interface to the heap.
At this stage we can be agnostic to the type of items in the array, although
most typically they will be bytes.

```cs
using System;

namespace JetBlack.Caching.Collections.Specialized
{
    public interface IHeap<T> : IDisposable
    {
        T[] Read(Handle handle);
        void Write(Handle handle, T[] bytes);
        Handle Allocate(long length);
        void Free(Handle handle);
        Block GetAllocatedBlock(Handle handle);
    }
}
```

### The Heap Manager Interface

The tricky bit in a heap is managing the free list. As allocations are requested and released, discarded blocks are gathered together to be re-allocated. With the interface used here (where access is managed through a handle) the data itself may be completely reorganised. This stops the heap growing unnecessarily. There are many algorithms available for this, so I decided to create an IHeapManager to represent allocation, and allow this part too be pluggable.

```cs
namespace JetBlack.Caching.Collections.Specialized
{
    public interface IHeapManager
    {
        Handle Allocate(long length);
        void Free(Handle handle);
        Block CreateFreeBlock(long minimumLength);
        Block GetAllocatedBlock(Handle handle);
        Block FindFreeBlock(long length);
        Block Fragment(Block freeBlock, long length);
    }
}
```

### The HeapManager implementation

My implementation of the heap manager is fairly trivial, but it was sufficient
for my purpose. The only optimisation it performs is to merge adjacent freed
blocks.

```cs
using System;
using System.Collections.Generic;
using System.Linq;

namespace JetBlack.Caching.Collections.Specialized
{
    public class LightweightHeapManager : IHeapManager
    {
        private readonly long _blockSize;
        private readonly IDictionary<Handle,Block> _allocatedBlocks = new Dictionary<Handle, Block>();
        private readonly ISet<Block> _freeBlocks = new HashSet<Block>();
        private long _length;

        public LightweightHeapManager(long blockSize = 2048)
        {
            _blockSize = blockSize;
        }

        public Handle Allocate(long length)
        {
            if (_freeBlocks.Count == 0)
                CreateFreeBlock(length);

            var block = FindFreeBlock(length);
            if (Equals(block, default(Block)))
                block = CreateFreeBlock(length);

            if (block.Length > length)
                block = Fragment(block, length);
            else
                _freeBlocks.Remove(block);

            _allocatedBlocks.Add(block.Handle, block);

            return block.Handle;
        }

        public void Free(Handle handle)
        {
            var block = GetAllocatedBlock(handle);
            _allocatedBlocks.Remove(handle);

            var previousAdjacentBlock = _freeBlocks.FirstOrDefault(x => x.Index + x.Length == block.Index);
            if (!Equals(previousAdjacentBlock, default(Block)))
            {
                _freeBlocks.Remove(previousAdjacentBlock);
                block = new Block(previousAdjacentBlock.Index, previousAdjacentBlock.Length + block.Length);
            }

            var endIndex = block.Index + block.Length;
            var followingAdjacentBlock = _freeBlocks.FirstOrDefault(x => x.Index == endIndex);
            if (!Equals(followingAdjacentBlock, default(Block)))
            {
                _freeBlocks.Remove(followingAdjacentBlock);
                block = new Block(block.Index, block.Length + followingAdjacentBlock.Length);
            }

            _freeBlocks.Add(block);
        }

        public Block CreateFreeBlock(long minimumLength)
        {
            var blocks = minimumLength / _blockSize;
            if (blocks * _blockSize < minimumLength)
                ++blocks;
            var length = blocks * _blockSize;
            var block = new Block(_length, length);
            _freeBlocks.Add(block);
            _length += length;
            return block;
        }

        public Block GetAllocatedBlock(Handle handle)
        {
            Block block;
            if (!_allocatedBlocks.TryGetValue(handle, out block))
                throw new Exception("invalid handle");
            return block;
        }

        public Block FindFreeBlock(long length)
        {
            var freeBlock = default(Block);
            foreach (var block in _freeBlocks.Where(x => x.Length >= length))
            {
                if (block.Length == length)
                    return block;

                if (Equals(freeBlock, default(Block)))
                    freeBlock = block;
                else if (block.Length < freeBlock.Length)
                    freeBlock = block;
            }

            return freeBlock;
        }

        public Block Fragment(Block freeBlock, long length)
        {
            if (freeBlock.Length < length) throw new ArgumentOutOfRangeException("length", "block too small");
            if (freeBlock.Length == length) return freeBlock;
            _freeBlocks.Remove(freeBlock);
            var block = new Block(freeBlock.Index + length, freeBlock.Length - length);
            _freeBlocks.Add(block);
            return new Block(freeBlock.Index, length);
        }

    }
}
```

### The Heap Implementation

With most of the heavy lifting done, the heap implementation looks pretty
trivial. The class is abstract, as we have yet to decide how to represent the
array.

```cs
namespace JetBlack.Caching.Collections.Specialized
{
    public abstract class Heap<T> : IHeap<T>
    {
        private readonly IHeapManager _heapManager;

        public Heap(IHeapManager heapManager)
        {
            _heapManager = heapManager;
        }

        public abstract T[] Read(Handle handle);

        public abstract void Write(Handle handle, T[] bytes);

        public Handle Allocate(long length)
        {
            return _heapManager.Allocate(length);
        }

        public void Free(Handle handle)
        {
            _heapManager.Free(handle);
        }

        protected virtual Block CreateFreeBlock(long minimumLength)
        {
            return _heapManager.CreateFreeBlock(minimumLength);
        }

        public Block GetAllocatedBlock(Handle handle)
        {
            return _heapManager.GetAllocatedBlock(handle);
        }

        public virtual void Dispose()
        {
        }
    }
}
```

### The StreamHeap

As my goal is to create a persistent cache the first step is to model the heap
with a byte stream. There a couple of non-obvious choices here. 

The first is the constructor. There is some confusion over the ownership of the
stream. Should this class dispose of the stream or not? Is it the owner? I
could have passed a flag in the public constructor, but it seemed more natural
that the class would own the stream if it created it. This is why I have a
factory method for stream construction.

Second we can see the reason for `GetAllocatedBlock` and `CreateFreeBlock`.
This class does the actual reading and writing, so it needs the information
provided by the heap to fulfil these duties. It also needs to know about the
free block creation, so it can manage the physical space.

```cs
using System;
using System.IO;

namespace JetBlack.Caching.Collections.Specialized
{
    public class StreamHeap : Heap<byte>
    {
        private readonly bool _isStreamOwner;
        public Stream Stream { get; protected set; }

        public StreamHeap(Stream stream, IHeapManager heapManager)
            : this(stream, false, heapManager)
        {
        }

        public StreamHeap(Func<Stream> streamFactory, IHeapManager heapManager)
            : this(streamFactory(), true, heapManager)
        {
        }

        protected StreamHeap(Stream stream, bool isStreamOwner, IHeapManager heapManager)
            : base(heapManager)
        {
            Stream = stream;
            _isStreamOwner = isStreamOwner;
        }

        public override byte[] Read(Handle handle)
        {
            var block = GetAllocatedBlock(handle);
            Stream.Position = block.Index;
            var buffer = new byte[block.Length];
            var count = buffer.Length;
            var offset = 0;
            while (count > 0)
            {
                var bytesRead = Stream.Read(buffer, offset, count);
                if (bytesRead == 0)
                    throw new EndOfStreamException();
                offset += bytesRead;
                count -= bytesRead;
            }
            return buffer;
        }

        public override void Write(Handle handle, byte[] bytes)
        {
            var block = GetAllocatedBlock(handle);
            if (block.Length != bytes.Length)
                throw new Exception("Invalid length");
            Stream.Position = block.Index;
            Stream.Write(bytes, 0, bytes.Length);
        }

        protected override Block CreateFreeBlock(long minimumLength)
        {
            var block = base.CreateFreeBlock(minimumLength);
            Stream.SetLength(block.Index + block.Length);
            return block;
        }

        public override void Dispose()
        {
            if (_isStreamOwner)
                Stream.Dispose();
        }
    }
}
```

### The FileStreamHeap

Finally we can save it to disc!

The same strategy (this time over file ownership) is used, with a factory class
indicating ownership. 

```cs
using System;
using System.IO;

namespace JetBlack.Caching.Collections.Specialized
{
    public class FileStreamHeap : StreamHeap
    {
        private readonly bool _isFileOwner;

        public FileStreamHeap(FileStream stream, IHeapManager heapManager)
            : this(stream, false, false, heapManager)
        {
        }

        public FileStreamHeap(Func<FileStream> fileStreamFactory, IHeapManager heapManager)
            : this(fileStreamFactory(), true, true, heapManager)
        {
        }

        protected FileStreamHeap(FileStream stream, bool isStreamOwner, bool isFileOwner, IHeapManager heapManager)
            : base(stream, isStreamOwner, heapManager)
        {
            _isFileOwner = isFileOwner;
        }

        public override void Dispose()
        {
            base.Dispose();

            if (_isFileOwner)
                File.Delete(((FileStream)Stream).Name);
        }
    }
}
```

### Tests For the Stream Heap

There follow some tests for the stream heap. They also demonstrate its usage.

```cs
using System.IO;
using NUnit.Framework;
using JetBlack.Patterns.Heaps;
using System.Text;

namespace JetBlack.Caching.Test.Collections.Specialized
{
    [TestFixture]
    public class StreamHeapFixture
    {
        [Test]
        public void Test()
        {
            var stream = new MemoryStream();
            var heap = new StreamHeap(stream, new LightweightHeapManager(8));

            const string oneText = "One";
            var oneHandle = AllocateAndWrite(heap, oneText);

            const string twoText = "Two";
            var twoHandle = AllocateAndWrite(heap, twoText);

            const string threeText = "Three";
            var threeHandle = AllocateAndWrite(heap, threeText);

            const string fourText = "Four";
            var fourHandle = AllocateAndWrite(heap, fourText);

            const string fiveText = "Five";
            var fiveHandle = AllocateAndWrite(heap, fiveText);

            const string sixText = "Six";
            var sixHandle = AllocateAndWrite(heap, sixText);

            Assert.AreEqual(oneText, Read(heap, oneHandle));
            Assert.AreEqual(twoText, Read(heap, twoHandle));
            Assert.AreEqual(threeText, Read(heap, threeHandle));
            Assert.AreEqual(fourText, Read(heap, fourHandle));
            Assert.AreEqual(fiveText, Read(heap, fiveHandle));
            Assert.AreEqual(sixText, Read(heap, sixHandle));

            heap.Free(oneHandle);
            heap.Free(twoHandle);
            twoHandle = AllocateAndWrite(heap, twoText);
            oneHandle = AllocateAndWrite(heap, oneText);
            Assert.AreEqual(oneText, Read(heap, oneHandle));
            Assert.AreEqual(twoText, Read(heap, twoHandle));

            heap.Free(fourHandle);
            heap.Free(sixHandle);
            heap.Free(fiveHandle);
        }

        private static Handle AllocateAndWrite(IHeap<byte> heap, string text)
        {
            var bytes = Encoding.UTF8.GetBytes(text);
            var handle = heap.Allocate(bytes.Length);
            heap.Write(handle, bytes);
            return handle;
        }

        private static string Read(IHeap<byte> heap, Handle handle)
        {
            var bytes = heap.Read(handle);
            return Encoding.UTF8.GetString(bytes);
        }
    }
}
```

### Test For File Streams

Apart from the construction the file stream heap has exactly the same
functionality as the stream heap, so all I test here is the ownership.

```cs
using System;
using NUnit.Framework;
using System.IO;
using JetBlack.Patterns.Heaps;

namespace JetBlack.Caching.Test.Collections.Specialized
{
    [TestFixture]
    public class FileStreamHeapFixture
    {
        [Test]
        public void TestDispose()
        {
            Func<FileStream> fileStreamFactory = () => new FileStream(Path.GetTempFileName(), FileMode.Open);
            var heap = new FileStreamHeap(fileStreamFactory, new LightweightHeapManager(2048));
            var fileName = ((FileStream)heap.Stream).Name;
            Assert.IsTrue(File.Exists(fileName));
            heap.Dispose();
            Assert.IsFalse(File.Exists(fileName));
        }
    }
}
```

## Persistent Dictionary

This describes the implementation of a persistent dictionary. There are a
number of excellent solutions to this online, but most were highly complex.
This is a simple implementation which was sufficient for my purpose. It uses
the classes described in the Heap section.

### The Cache

First we defined the interface for the persistent cache. This follows the
traditional CRUD pattern.

```cs
using System;
using JetBlack.Patterns.Heaps;

namespace JetBlack.Patterns.Caching
{
    public interface ICache<T> : IDisposable
    {
        Handle Create(T value);
        T Read(Handle handle);
        Handle Update(Handle handle, T value);
        void Delete(Handle handle);
    }
}
```

### A Cache Implementation

Now we can implement a fairly straightforward cache, without making too many
decisions about how it will be used. All we need to provide is a heap, a
serializer, and a deserializer.

The `Update` method has some complexity. If the size of the object has changed
it will need to deallocate the old block and allocate a new one.

```cs
using System;
using JetBlack.Patterns.Heaps;

namespace JetBlack.Patterns.Caching
{
    public class SerializingCache<TItem, TRaw> : ICache<TItem>
    {
        private readonly IHeap<TRaw> _heap;
        private readonly Func<TItem, TRaw[]> _serialize;
        private readonly Func<TRaw[], TItem> _deserialize;

        public SerializingCache(IHeap<TRaw> heap, Func<TItem, TRaw[]> serialize, Func<TRaw[],TItem> deserialize)
        {
            _heap = heap;
            _serialize = serialize;
            _deserialize = deserialize;
        }

        public Handle Create(TItem value)
        {
            var raw = _serialize(value);
            var handle = _heap.Allocate(raw.Length);
            _heap.Write(handle, raw);
            return handle;
        }

        public TItem Read(Handle handle)
        {
            var raw = _heap.Read(handle);
            return _deserialize(raw);
        }

        public Handle Update(Handle handle, TItem value)
        {
            var raw = _serialize(value);
            var block = _heap.GetAllocatedBlock(handle);

            if (block.Length != raw.Length)
            {
                _heap.Free(handle);
                handle = _heap.Allocate(raw.Length);
            }

            _heap.Write(handle, raw);

            return handle;
        }

        public void Delete(Handle handle)
        {
            _heap.Free(handle);
        }

        public void Dispose()
        {
            _heap.Dispose();
        }
    }
}
```

### An Example String Cache

A trivial implementation of the serializers could be the following.

```cs
using System.Text;
using JetBlack.Patterns.Heaps;

namespace JetBlack.Patterns.Caching
{
    public class StringCache : SerializingCache<string,byte>
    {
        public StringCache(IHeap<byte> heap, Encoding encoding)
            : base(heap, encoding.GetBytes, encoding.GetString)
        {
        }

        public StringCache(IHeap<byte> heap)
            : this(heap, Encoding.Default)
        {           
        }
    }
}
```

### A Persistant Dictionary

All we need to do to create a persistent dictionary is to wrap a cache with a
dictionary implementation. We keep and index of keys to handles to map to the
persistent cache.

The factory class at the top generates the dictionary with binary serializers
so we can support a large population of possible objects.

```cs
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBlack.Core.Collections.Generic;
using JetBlack.Patterns.Heaps;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace JetBlack.Patterns.Caching
{
    public static class PersistentDictionary
    {
        public static readonly BinaryFormatter BinaryFormatter = new BinaryFormatter();

        public static PersistantDictionary<TKey, TValue> Create<TKey,TValue>()
        {
            return Create<TKey,TValue>(new FileStreamHeap(() => new FileStream(Path.GetTempFileName(), FileMode.Open), new LightweightHeapManager()));
        }

        public static PersistantDictionary<TKey,TValue> Create<TKey,TValue>(IHeap<byte> heap)
        {
            return new PersistantDictionary<TKey,TValue>(new SerializingCache<TValue,byte>(heap, Serialize, Deserialize<TValue>));
        }

        public static byte[] Serialize<TValue>(TValue value)
        {
            using (var stream = new MemoryStream())
            {
                BinaryFormatter.Serialize(stream, value);
                stream.Flush();
                return stream.GetBuffer();
            }
        }

        public static TValue Deserialize<TValue>(byte[] bytes)
        {
            using (var stream = new MemoryStream())
            {
                return (TValue)BinaryFormatter.Deserialize(stream);
            }
        }

    }

    public class PersistantDictionary<TKey, TValue> : IDictionary<TKey, TValue>, IDisposable
    {
        private readonly ICache<TValue> _cache;
        private readonly IDictionary<TKey, Handle> _index = new Dictionary<TKey, Handle>();

        public PersistantDictionary(ICache<TValue> cache)
        {
            _cache = cache;
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            using (var indexEnumerator = _index.GetEnumerator())
            {
                while (indexEnumerator.MoveNext())
                    yield return KeyValuePair.Create(indexEnumerator.Current.Key, _cache.Read(indexEnumerator.Current.Value));
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(KeyValuePair<TKey,TValue> item)
        {
            Add(item.Key, item.Value);
        }

        public void Clear()
        {
            foreach (var handle in _index.Values)
                _cache.Delete(handle);
            _index.Clear();
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return ContainsKey(item.Key);
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            foreach (var item in _index)
                array[arrayIndex++] = KeyValuePair.Create(item.Key, _cache.Read(item.Value));
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            return Remove(item.Key);
        }

        public int Count
        {
            get { return _index.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool ContainsKey(TKey key)
        {
            return _index.ContainsKey(key);
        }

        public void Add(TKey key, TValue value)
        {
            var handle = _cache.Create(value);
            _index.Add(key, handle);
        }

        public bool Remove(TKey key)
        {
            Handle handle;
            if (!_index.TryGetValue(key, out handle))
                return false;
            _cache.Delete(handle);
            _index.Remove(key);
            return true;
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            Handle handle;
            if (!_index.TryGetValue(key, out handle))
            {
                value = default(TValue);
                return false;
            }
            value = _cache.Read(handle);
            return true;
        }

        public TValue this[TKey key]
        {
            get { return _cache.Read(_index[key]); }
            set
            {
                Handle handle;
                _index[key] = _index.TryGetValue(key, out handle) ? _cache.Update(handle, value) : _cache.Create(value);
            }
        }

        public ICollection<TKey> Keys
        {
            get { return _index.Keys; }
        }

        public ICollection<TValue> Values
        {
            get { return _index.Values.Select(handle => _cache.Read(handle)).ToList(); }
        }

        public void Dispose()
        {
            _cache.Dispose();
        }
    }
}
```

## Caching Dictionary

This section describes an implementation of a local/persistent caching
dictionary bringing together the classes discussed in the Heap,
PersistentDictionary, and CircularBuffer sections.

### Design

The implementation uses an in memory dictionary and a persistent dictionary.
The recently accessed items remain in the local dictionary, while the less
used are moved to the persistent store. As older values are accessed they
are moved back into the local store.

```cs
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBlack.Core.Collections.Generic;

namespace JetBlack.Patterns.Caching
{
    public class CachingDictionry<TKey,TValue> : IDictionary<TKey,TValue>, IDisposable
    {
        private readonly PersistantDictionary<TKey,TValue> _persistantDictionary;
        private readonly IDictionary<TKey, TValue> _localDictionary;
        private readonly ICircularBuffer<TKey> _localKeyQueue;

        public CachingDictionry(PersistantDictionary<TKey, TValue> persistantDictionary, int maxCacheCount)
        {
            _persistantDictionary = persistantDictionary;
            _localDictionary = new Dictionary<TKey, TValue>(maxCacheCount);
            _localKeyQueue = new CircularBuffer<TKey>(maxCacheCount);
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            foreach (var item in _localDictionary)
                yield return item;
            foreach (var item in _persistantDictionary)
                yield return item;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            Add(item.Key, item.Value);
        }

        public void Clear()
        {
            _localDictionary.Clear();
            _persistantDictionary.Clear();
            _localKeyQueue.Clear();
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return ContainsKey(item.Key);
        }

        public void CopyTo(KeyValuePair<TKey,TValue>[] array, int arrayIndex)
        {
            _localDictionary.CopyTo(array, arrayIndex);
            _persistantDictionary.CopyTo(array, _localDictionary.Count + arrayIndex);
        }

        public bool Remove(KeyValuePair<TKey,TValue> item)
        {
            return Remove(item.Key);
        }

        public int Count
        {
            get { return _localDictionary.Count + _persistantDictionary.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool ContainsKey(TKey key)
        {
            return _localDictionary.ContainsKey(key) || _persistantDictionary.ContainsKey(key);
        }

        public void Add(TKey key, TValue value)
        {
            _localDictionary.Add(key, value);
            var overwrittenKey = _localKeyQueue.Enqueue(key);
            if (!Equals(overwrittenKey, default(TKey)))
                MakePersistant(overwrittenKey);
        }

        public bool Remove(TKey key)
        {
            var status = _localDictionary.Remove(key);
            if (status)
                _localKeyQueue.RemoveAt(_localKeyQueue.IndexOf(key));
            else
                status = _persistantDictionary.Remove(key);
            return status;
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            if (!_localDictionary.TryGetValue(key, out value))
            {
                if (!_persistantDictionary.TryGetValue(key, out value))
                {
                    value = default(TValue);
                    return false;
                }

                MakeLocal(key);
                value = _localDictionary[key];
            }
            return true;
        }

        public TValue this[TKey key]
        {
            get
            {
                if (Equals(key, null))
                    throw new ArgumentNullException();

                TValue value;
                if (!TryGetValue(key, out value))
                    throw new KeyNotFoundException();
                return value;
            }
            set
            {
                if (Equals(key, null))
                    throw new ArgumentNullException();

                if (_localDictionary.ContainsKey(key))
                    _localDictionary[key] = value;
                else if (!_persistantDictionary.ContainsKey(key))
                    Add(key, value);
                else
                {
                    MakeLocal(key);
                    _localDictionary[key] = value;
                }
            }
        }

        public ICollection<TKey> Keys
        {
            get { return _localDictionary.Keys.Concat(_persistantDictionary.Keys).ToList(); }
        }

        public ICollection<TValue> Values
        {
            get { return _localDictionary.Values.Concat(_persistantDictionary.Values).ToList(); }
        }

        private void MakeLocal(TKey key)
        {
            Move(key, _persistantDictionary, _localDictionary);

            var overwrittenKey = _localKeyQueue.Enqueue(key);
            if (!Equals(overwrittenKey, default(TKey)))
                MakePersistant(overwrittenKey);
        }

        private void MakePersistant(TKey key)
        {
            Move(key, _localDictionary, _persistantDictionary);
        }

        private static void Move(TKey key, IDictionary<TKey, TValue> from, IDictionary<TKey, TValue> to)
        {
            var value = from[key];
            from.Remove(key);
            to.Add(key, value);
        }

        public void Dispose()
        {
            _persistantDictionary.Dispose();
        }
    }
}
```