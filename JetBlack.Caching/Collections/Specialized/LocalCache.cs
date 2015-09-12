﻿using System.Collections;
using System.Collections.Generic;
using JetBlack.Caching.Collections.Generic;

namespace JetBlack.Caching.Collections.Specialized
{
    public class LocalCache<TKey, TValue> : ILocalCache<TKey,TValue>
    {
        private readonly IDictionary<TKey, TValue> _localDictionary;
        private readonly ICircularBuffer<TKey> _localKeyQueue;

        public LocalCache(int capacity)
        {
            _localDictionary = new Dictionary<TKey, TValue>(capacity);
            _localKeyQueue = new CircularBuffer<TKey>(capacity);
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return _localDictionary.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Clear()
        {
            _localDictionary.Clear();
            _localKeyQueue.Clear();
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            _localDictionary.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return _localDictionary.Count; }
        }

        public bool ContainsKey(TKey key)
        {
            return _localDictionary.ContainsKey(key);
        }

        public bool AddOrOverwrite(TKey key, TValue value, out KeyValuePair<TKey,TValue> overwritten)
        {
            var isFull = _localKeyQueue.Count == _localKeyQueue.Capacity;
            
            var overwrittenKey = _localKeyQueue.Enqueue(key);
            
            if (isFull)
            {
                var overwrittenValue = _localDictionary[overwrittenKey];
                _localDictionary.Remove(overwrittenKey);
                _localDictionary.Add(key, value);
                overwritten = KeyValuePair.Create(overwrittenKey, overwrittenValue);
                return true;
            }
            
            _localDictionary.Add(key, value);
            overwritten = default(KeyValuePair<TKey, TValue>);
            return false;
        }

        public bool Remove(TKey key)
        {
            var status = _localDictionary.Remove(key);
            if (status)
                _localKeyQueue.RemoveAt(_localKeyQueue.IndexOf(key));
            return status;
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            if (!_localDictionary.TryGetValue(key, out value))
                return false;

            TryMakeFirst(key);
            return true;
        }

        public TValue this[TKey key]
        {
            get { return Get(key); }
            set { Set(key, value); }
        }

        public ICollection<TKey> Keys
        {
            get { return _localDictionary.Keys; }
        }

        public ICollection<TValue> Values
        {
            get { return _localDictionary.Values; }
        }

        private TValue Get(TKey key)
        {
            TryMakeFirst(key);
            return _localDictionary[key];
        }

        private void Set(TKey key, TValue value)
        {
            TryMakeFirst(key);
            _localDictionary[key] = value;
        }

        private bool TryMakeFirst(TKey key)
        {
            if (_localKeyQueue.Count == 0)
                return false;

            if (_localKeyQueue.Count == 1)
                return Equals(_localKeyQueue[0], key);

            var index = _localKeyQueue.IndexOf(key);
            if (index == -1)
                return false;

            _localKeyQueue.RemoveAt(index);
            _localKeyQueue.Enqueue(key);
            return true;
        }
    }
}