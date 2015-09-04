using System.IO;
using System.Text;
using JetBlack.Caching.Collections.Specialized;
using NUnit.Framework;

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
