using System;
using System.IO;
using JetBlack.Caching.Collections.Specialized;
using NUnit.Framework;

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
