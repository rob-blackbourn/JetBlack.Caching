using System;

namespace JetBlack.Caching.Collections.Specialized
{
    public interface ICache<T> : IDisposable
    {
        Handle Create(T value);
        T Read(Handle handle);
        Handle Update(Handle handle, T value);
        void Delete(Handle handle);
    }
}
