using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using JetBlack.Caching.Collections.Generic;

namespace JetBlack.Caching.Collections.Specialized
{
    public static class PersistentDictionary
    {
        public static readonly BinaryFormatter BinaryFormatter = new BinaryFormatter();

        public static PersistantDictionary<TKey, TValue> Create<TKey, TValue>()
        {
            return Create<TKey, TValue>(new FileStreamHeap(() => new FileStream(Path.GetTempFileName(), FileMode.Open), new LightweightHeapManager()));
        }

        public static PersistantDictionary<TKey, TValue> Create<TKey, TValue>(IHeap<byte> heap)
        {
            return new PersistantDictionary<TKey, TValue>(new SerializingCache<TValue, byte>(heap, Serialize, Deserialize<TValue>));
        }

        public static byte[] Serialize<TValue>(TValue value)
        {
            using (var stream = new MemoryStream())
            {
                BinaryFormatter.Serialize(stream, value);
                stream.Flush();
                return stream.GetBuffer();
            }
        }

        public static TValue Deserialize<TValue>(byte[] bytes)
        {
            using (var stream = new MemoryStream())
            {
                return (TValue)BinaryFormatter.Deserialize(stream);
            }
        }

    }

    public class PersistantDictionary<TKey, TValue> : IDictionary<TKey, TValue>, IDisposable
    {
        private readonly ICache<TValue> _cache;
        private readonly IDictionary<TKey, Handle> _index = new Dictionary<TKey, Handle>();

        public PersistantDictionary(ICache<TValue> cache)
        {
            _cache = cache;
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            using (var indexEnumerator = _index.GetEnumerator())
            {
                while (indexEnumerator.MoveNext())
                    yield return KeyValuePair.Create(indexEnumerator.Current.Key, _cache.Read(indexEnumerator.Current.Value));
            }
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
            foreach (var handle in _index.Values)
                _cache.Delete(handle);
            _index.Clear();
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return ContainsKey(item.Key);
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            foreach (var item in _index)
                array[arrayIndex++] = KeyValuePair.Create(item.Key, _cache.Read(item.Value));
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            return Remove(item.Key);
        }

        public int Count
        {
            get { return _index.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool ContainsKey(TKey key)
        {
            return _index.ContainsKey(key);
        }

        public void Add(TKey key, TValue value)
        {
            var handle = _cache.Create(value);
            _index.Add(key, handle);
        }

        public bool Remove(TKey key)
        {
            Handle handle;
            if (!_index.TryGetValue(key, out handle))
                return false;
            _cache.Delete(handle);
            _index.Remove(key);
            return true;
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            Handle handle;
            if (!_index.TryGetValue(key, out handle))
            {
                value = default(TValue);
                return false;
            }
            value = _cache.Read(handle);
            return true;
        }

        public TValue this[TKey key]
        {
            get { return _cache.Read(_index[key]); }
            set
            {
                Handle handle;
                _index[key] = _index.TryGetValue(key, out handle) ? _cache.Update(handle, value) : _cache.Create(value);
            }
        }

        public ICollection<TKey> Keys
        {
            get { return _index.Keys; }
        }

        public ICollection<TValue> Values
        {
            get { return _index.Values.Select(handle => _cache.Read(handle)).ToList(); }
        }

        public void Dispose()
        {
            _cache.Dispose();
        }
    }
}
