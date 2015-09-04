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
var value = buffer[3];
buffer.RemoveAt(3);
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

All the obvious candidates are in the interface [`ICircularBuffer<T>`](https://github.com/rob-blackbourn/JetBlack.Caching/blob/master/JetBlack.Caching/Collections/Generic/ICircularBuffer.cs).
You can see the queue style interaction. Also note the enqueue returns the
overwritten value if one exists (otherwise it will be `default(T)`). The
indexer methods, `IndexOf`, `InsertAt`, and `RemoveAt` provide the
mechanism to manipulate the queue directly.

I could have provided item lookups by value rather than index, but these would
have still required the indexing operators and I wanted to keep the class small.
It should be clear how a derived class, (possibly implementing `IList<T>`)
could be trivially implemented.

### The Implementation

The code for [`CircularBuffer<T>`](https://github.com/rob-blackbourn/JetBlack.Caching/blob/master/JetBlack.Caching/Collections/Generic/CircularBuffer.cs)
follows the traditional circular buffer pattern of declaring a fixed
length array, then maintaining an index to the head and tail of the array.
Typically the size of the buffer is defined by the constructor, but I have
included a Capacity property, to allow more sympathetic subclassing.

Though not strictley necessary it seemed convenient to implement `IEnumerable<T>`.
The amount of code required is small, and it provides Linq compatibility at
little extra cost.

### Tests

Here are some simple [`tests`](https://github.com/rob-blackbourn/JetBlack.Caching/blob/master/JetBlack.Caching.Test/Collections/Generic/CircularBufferFixture.cs) which also demonstrate how to use the class.

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

The [`Handle`](https://github.com/rob-blackbourn/JetBlack.Caching/blob/master/JetBlack.Caching/Collections/Specialized/Handle.cs)
is irritatingly large for such a trivial data structure. This is
because it implements a couple of interfaces for equality, and a factory class
for generating new handles.

#### Block

The [`Block`](https://github.com/rob-blackbourn/JetBlack.Caching/blob/master/JetBlack.Caching/Collections/Specialized/Block.cs)
is rather more terse. It has the index offset into the heap, and a
length. Both the handle and the block are immutable.

### The Heap Interface

Now we have our basic data structures we can define the interface [`IHeap<T>`](https://github.com/rob-blackbourn/JetBlack.Caching/blob/master/JetBlack.Caching/Collections/Specialized/IHeap.cs).
At this stage we can be agnostic to the type of items in the array, although
most typically they will be bytes.

### The Heap Manager Interface

The tricky bit in a heap is managing the free list. As allocations are requested
and released, discarded blocks are gathered together to be re-allocated. With
the interface used here (where access is managed through a handle) the data
itself may be completely reorganised. This stops the heap growing unnecessarily.
There are many algorithms available for this, so I decided to create an
[`IHeapManager`](https://github.com/rob-blackbourn/JetBlack.Caching/blob/master/JetBlack.Caching/Collections/Specialized/IHeapManager.cs)
to represent allocation, and allow this part too be pluggable.

### The HeapManager implementation

My implementation of the heap manager ([`LightweightHeapManager`](https://github.com/rob-blackbourn/JetBlack.Caching/blob/master/JetBlack.Caching/Collections/Specialized/LightweightHeapManager.cs))
is fairly trivial, but it was sufficient for my purpose. The only optimisation
it performs is to merge adjacent freed blocks.

### The Heap Implementation

With most of the heavy lifting done, the [`Heap<T>`](https://github.com/rob-blackbourn/JetBlack.Caching/blob/master/JetBlack.Caching/Collections/Specialized/Heap.cs)
implementation looks pretty trivial. The class is abstract, as we have yet to
decide how to represent the array.

### The StreamHeap

As my goal is to create a persistent cache the first step is to model the heap
with a byte stream ([`StreamHeap`](https://github.com/rob-blackbourn/JetBlack.Caching/blob/master/JetBlack.Caching/Collections/Specialized/StreamHeap.cs)).
There a couple of non-obvious choices here. 

The first is the constructor. There is some confusion over the ownership of the
stream. Should this class dispose of the stream or not? Is it the owner? I
could have passed a flag in the public constructor, but it seemed more natural
that the class would own the stream if it created it. This is why I have a
factory method for stream construction.

Second we can see the reason for `GetAllocatedBlock` and `CreateFreeBlock`.
This class does the actual reading and writing, so it needs the information
provided by the heap to fulfil these duties. It also needs to know about the
free block creation, so it can manage the physical space.

### The FileStreamHeap

Finally we can save it to disc with the [`FileStreamHeap`](https://github.com/rob-blackbourn/JetBlack.Caching/blob/master/JetBlack.Caching/Collections/Specialized/FileStreamHeap.cs).

The same strategy (this time over file ownership) is used, with a factory class
indicating ownership. 

### Tests For the Stream Heap

There follow some [tests](https://github.com/rob-blackbourn/JetBlack.Caching/blob/master/JetBlack.Caching.Test/Collections/Specialized/StreamHeapFixture.cs)
for the stream heap. They also demonstrate its usage.

### Test For File Streams

Apart from the construction the file stream heap has exactly the same
functionality as the stream heap, so all I [test](https://github.com/rob-blackbourn/JetBlack.Caching/blob/master/JetBlack.Caching.Test/Collections/Specialized/FileStreamHeapFixture.cs) here is the ownership.

## Persistent Dictionary

This describes the implementation of a persistent dictionary. There are a
number of excellent solutions to this online, but most were highly complex.
This is a simple implementation which was sufficient for my purpose. It uses
the classes described in the Heap section.

### The Cache

First we defined the interface for the persistent cache, [`ICache<T>`](https://github.com/rob-blackbourn/JetBlack.Caching/blob/master/JetBlack.Caching/Collections/Specialized/ICache.cs).
This follows the traditional CRUD pattern.

### A Cache Implementation

Now we can implement a fairly straightforward cache [`SerializingCache`](https://github.com/rob-blackbourn/JetBlack.Caching/blob/master/JetBlack.Caching/Collections/Specialized/SerializingCache.cs),
without making too many decisions about how it will be used. All we need 
to provide is a heap, a serializer, and a deserializer.

The `Update` method has some complexity. If the size of the object has changed
it will need to deallocate the old block and allocate a new one.

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

All we need to do to create a [`PersistentDictionary`](https://github.com/rob-blackbourn/JetBlack.Caching/blob/master/JetBlack.Caching/Collections/Specialized/PersistentDictionary.cs)
is to wrap a cache with a dictionary implementation. We keep and
index of keys to handles to map to the persistent cache.

The factory class at the top generates the dictionary with binary serializers
so we can support a large population of possible objects.

## Caching Dictionary

This section describes an implementation of a local/persistent caching
dictionary bringing together the classes discussed in the Heap,
PersistentDictionary, and CircularBuffer sections.

### Design

The implementation uses an in memory dictionary and a persistent dictionary to
create the [`CachingDictionary<TKey,TValue>`](https://github.com/rob-blackbourn/JetBlack.Caching/blob/master/JetBlack.Caching/Collections/Specialized/CachingDictionary.cs).
The recently accessed items remain in the local dictionary, while the less
used are moved to the persistent store. As older values are accessed they
are moved back into the local store.
