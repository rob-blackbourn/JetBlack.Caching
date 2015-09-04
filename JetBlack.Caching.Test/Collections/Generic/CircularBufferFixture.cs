using System.Diagnostics;
using System.Linq;
using JetBlack.Caching.Collections.Generic;
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

        [Test, Ignore]
        public void UsageExample()
        {
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
        }
    }
}
