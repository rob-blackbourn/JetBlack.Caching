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
