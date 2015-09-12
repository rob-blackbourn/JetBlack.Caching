using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace JetBlack.Caching.Collections.Specialized
{
    public class CachingDictionary<TKey, TValue> : IDictionary<TKey, TValue>, IDisposable
    {
        private readonly PersistantDictionary<TKey, TValue> _persistantDictionary;
        private readonly ILocalCache<TKey, TValue> _localCache;

        public CachingDictionary(PersistantDictionary<TKey, TValue> persistantDictionary, ILocalCache<TKey,TValue> localCache)
        {
            _persistantDictionary = persistantDictionary;
            _localCache = localCache;
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            foreach (var item in _localCache)
                yield return item;
            foreach (var item in _persistantDictionary)
                yield return item;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            Add(item.Key, item.Value);
        }

        public void Clear()
        {
            _localCache.Clear();
            _persistantDictionary.Clear();
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return ContainsKey(item.Key);
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            _localCache.CopyTo(array, arrayIndex);
            _persistantDictionary.CopyTo(array, _localCache.Count + arrayIndex);
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            return Remove(item.Key);
        }

        public int Count
        {
            get { return _localCache.Count + _persistantDictionary.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool ContainsKey(TKey key)
        {
            return _localCache.ContainsKey(key) || _persistantDictionary.ContainsKey(key);
        }

        public void Add(TKey key, TValue value)
        {
            KeyValuePair<TKey, TValue> overwritten;
            if (_localCache.AddOrOverwrite(key, value, out overwritten))
                _persistantDictionary.Add(overwritten.Key, overwritten.Value);
        }

        public bool Remove(TKey key)
        {
            var status = _localCache.Remove(key);
            if (!status)
                status = _persistantDictionary.Remove(key);
            return status;
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            if (_localCache.TryGetValue(key, out value))
                return true;

            if (!_persistantDictionary.TryGetValue(key, out value))
            {
                value = default(TValue);
                return false;
            }
            
            MakeLocal(key, value);

            return true;
        }

        public TValue this[TKey key]
        {
            get
            {
                if (Equals(key, null))
                    throw new ArgumentNullException();

                TValue value;
                if (!TryGetValue(key, out value))
                    throw new KeyNotFoundException();
                return value;
            }
            set
            {
                if (Equals(key, null))
                    throw new ArgumentNullException();

                if (_localCache.ContainsKey(key))
                    _localCache[key] = value;
                else if (!_persistantDictionary.ContainsKey(key))
                    Add(key, value);
                else
                {
                    MakeLocal(key, value);
                    _localCache[key] = value;
                }
            }
        }

        public ICollection<TKey> Keys
        {
            get { return _localCache.Keys.Concat(_persistantDictionary.Keys).ToList(); }
        }

        public ICollection<TValue> Values
        {
            get { return _localCache.Values.Concat(_persistantDictionary.Values).ToList(); }
        }

        private void MakeLocal(TKey key, TValue value)
        {
            _persistantDictionary.Remove(key);

            KeyValuePair<TKey,TValue> overwritten;
            if (_localCache.AddOrOverwrite(key, value, out overwritten))
                _persistantDictionary.Add(overwritten.Key, overwritten.Value);
        }

        public void Dispose()
        {
            _persistantDictionary.Dispose();
        }
    }
}
