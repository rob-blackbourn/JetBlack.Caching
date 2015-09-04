namespace JetBlack.Caching.Collections.Specialized
{
    public abstract class Heap<T> : IHeap<T>
    {
        private readonly IHeapManager _heapManager;

        protected Heap(IHeapManager heapManager)
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
