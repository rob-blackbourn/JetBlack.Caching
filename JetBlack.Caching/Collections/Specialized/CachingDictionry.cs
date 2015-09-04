using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBlack.Caching.Collections.Generic;

namespace JetBlack.Caching.Collections.Specialized
{
    public class CachingDictionry<TKey, TValue> : IDictionary<TKey, TValue>, IDisposable
    {
        private readonly PersistantDictionary<TKey, TValue> _persistantDictionary;
        private readonly IDictionary<TKey, TValue> _localDictionary;
        private readonly ICircularBuffer<TKey> _localKeyQueue;

        public CachingDictionry(PersistantDictionary<TKey, TValue> persistantDictionary, int maxCacheCount)
        {
            _persistantDictionary = persistantDictionary;
            _localDictionary = new Dictionary<TKey, TValue>(maxCacheCount);
            _localKeyQueue = new CircularBuffer<TKey>(maxCacheCount);
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            foreach (var item in _localDictionary)
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
            _localDictionary.Clear();
            _persistantDictionary.Clear();
            _localKeyQueue.Clear();
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return ContainsKey(item.Key);
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            _localDictionary.CopyTo(array, arrayIndex);
            _persistantDictionary.CopyTo(array, _localDictionary.Count + arrayIndex);
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            return Remove(item.Key);
        }

        public int Count
        {
            get { return _localDictionary.Count + _persistantDictionary.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool ContainsKey(TKey key)
        {
            return _localDictionary.ContainsKey(key) || _persistantDictionary.ContainsKey(key);
        }

        public void Add(TKey key, TValue value)
        {
            _localDictionary.Add(key, value);
            var overwrittenKey = _localKeyQueue.Enqueue(key);
            if (!Equals(overwrittenKey, default(TKey)))
                MakePersistant(overwrittenKey);
        }

        public bool Remove(TKey key)
        {
            var status = _localDictionary.Remove(key);
            if (status)
                _localKeyQueue.RemoveAt(_localKeyQueue.IndexOf(key));
            else
                status = _persistantDictionary.Remove(key);
            return status;
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            if (!_localDictionary.TryGetValue(key, out value))
            {
                if (!_persistantDictionary.TryGetValue(key, out value))
                {
                    value = default(TValue);
                    return false;
                }

                MakeLocal(key);
                value = _localDictionary[key];
            }
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

                if (_localDictionary.ContainsKey(key))
                    _localDictionary[key] = value;
                else if (!_persistantDictionary.ContainsKey(key))
                    Add(key, value);
                else
                {
                    MakeLocal(key);
                    _localDictionary[key] = value;
                }
            }
        }

        public ICollection<TKey> Keys
        {
            get { return _localDictionary.Keys.Concat(_persistantDictionary.Keys).ToList(); }
        }

        public ICollection<TValue> Values
        {
            get { return _localDictionary.Values.Concat(_persistantDictionary.Values).ToList(); }
        }

        private void MakeLocal(TKey key)
        {
            Move(key, _persistantDictionary, _localDictionary);

            var overwrittenKey = _localKeyQueue.Enqueue(key);
            if (!Equals(overwrittenKey, default(TKey)))
                MakePersistant(overwrittenKey);
        }

        private void MakePersistant(TKey key)
        {
            Move(key, _localDictionary, _persistantDictionary);
        }

        private static void Move(TKey key, IDictionary<TKey, TValue> from, IDictionary<TKey, TValue> to)
        {
            var value = from[key];
            from.Remove(key);
            to.Add(key, value);
        }

        public void Dispose()
        {
            _persistantDictionary.Dispose();
        }
    }
}
