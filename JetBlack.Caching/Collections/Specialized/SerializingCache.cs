using System;

namespace JetBlack.Caching.Collections.Specialized
{
    public class SerializingCache<TItem, TRaw> : ICache<TItem>
    {
        private readonly IHeap<TRaw> _heap;
        private readonly Func<TItem, TRaw[]> _serialize;
        private readonly Func<TRaw[], TItem> _deserialize;

        public SerializingCache(IHeap<TRaw> heap, Func<TItem, TRaw[]> serialize, Func<TRaw[], TItem> deserialize)
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
