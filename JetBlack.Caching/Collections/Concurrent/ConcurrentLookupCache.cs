using System;
using System.Collections.Generic;

namespace JetBlack.Caching.Collections.Concurrent
{
    public class ConcurrentLookupCache<TKey, TValue>
    {
        public delegate bool TryLookup(TKey key, out TValue value);

        private readonly IDictionary<TKey, object> _keySpecificLocks = new Dictionary<TKey, object>(); 
        private readonly IDictionary<TKey, TValue> _cache = new Dictionary<TKey, TValue>();
        private readonly TryLookup _tryLookup;

        public ConcurrentLookupCache(TryLookup tryLookup)
            : this(tryLookup, null, new Dictionary<TKey, TValue>())
        {
        }

        public ConcurrentLookupCache(TryLookup tryLookup, Func<IEnumerable<KeyValuePair<TKey, TValue>>> cacheLoader, IDictionary<TKey, TValue> cache)
        {
            if (tryLookup == null) throw new ArgumentNullException("tryLookup");
            _tryLookup = tryLookup;

            _cache = cache;

            if (cacheLoader != null)
            {
                foreach (var item in cacheLoader())
                    _cache.Add(item.Key, item.Value);
            }
        }

        public virtual TValue Get(TKey key)
        {
            TValue value;
            if (TryGetValue(key, out value))
                return value;

            var keySpecificLock = GetKeySpecificLock(key);

            lock (keySpecificLock)
            {
                // Check for race condition.
                if (TryGetValue(key, out value))
                    return value;

                if (_tryLookup(key, out value))
                {
                    Add(key, value);
                }

                RemoveKeySpecificLock(key);

                return value;
            }
        }

        private bool TryGetValue(TKey key, out TValue value)
        {
            lock (_cache)
            {
                return _cache.TryGetValue(key, out value);
            }
        }

        private void Add(TKey key, TValue value)
        {
            lock (_cache)
            {
                _cache.Add(key, value);
            }
        }

        private object GetKeySpecificLock(TKey key)
        {
            lock (_keySpecificLocks)
            {
                object keySpecificLock;
                if (!_keySpecificLocks.TryGetValue(key, out keySpecificLock))
                    _keySpecificLocks.Add(key, keySpecificLock = new object());
                return keySpecificLock;
            }
        }

        private void RemoveKeySpecificLock(TKey key)
        {
            lock (_keySpecificLocks)
            {
                _keySpecificLocks.Remove(key);
            }
        }
    }
}
