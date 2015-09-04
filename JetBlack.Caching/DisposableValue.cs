using System;

namespace JetBlack.Caching
{
    public class DisposableValue<T> : IDisposable
    {
        private readonly Action _dispose;

        public DisposableValue(T value, Action dispose)
        {
            _dispose = dispose;
            Value = value;
        }

        public T Value { get; private set; }

        public void Dispose()
        {
            _dispose();
        }

        public static DisposableValue<T> Create(T value, Action dispose)
        {
            return new DisposableValue<T>(value, dispose);
        }
    }
}
