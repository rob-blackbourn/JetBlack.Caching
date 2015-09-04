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
