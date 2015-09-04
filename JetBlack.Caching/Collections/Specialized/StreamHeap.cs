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
