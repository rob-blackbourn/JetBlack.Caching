using System;
using System.Collections.Generic;
using JetBlack.Caching.Collections.Generic;
using JetBlack.Caching.Timing;

namespace JetBlack.Caching.Collections.Concurrent
{
    public class ConcurrentTimeoutDictionary<TKey, TValue> : TimeoutDictionary<TKey, TValue>
    {
        public ConcurrentTimeoutDictionary(IDateTimeProvider dateTimeProvider, TimeSpan timeout)
            : base(dateTimeProvider, timeout)
        {
        }

        public override void Add(TKey key, TValue value)
        {
            lock (this)
            {
                base.Add(key, value);
            }
        }

        public override void Clear()
        {
            lock (this)
            {
                base.Clear();
            }
        }

        public override bool Contains(KeyValuePair<TKey, TValue> item)
        {
            lock (this)
            {
                return base.Contains(item);
            }
        }

        public override bool ContainsKey(TKey key)
        {
            lock (this)
            {
                return base.ContainsKey(key);
            }
        }

        public override void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            lock (this)
            {
                base.CopyTo(array, arrayIndex);
            }
        }

        public override int Count
        {
            get
            {
                lock (this)
                {
                    return base.Count;
                }
            }
        }

        public override IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            lock (this)
            {
                return base.GetEnumerator();
            }
        }

        public override ICollection<TKey> Keys
        {
            get
            {
                lock (this)
                {
                    return base.Keys;
                }
            }
        }

        public override bool Remove(KeyValuePair<TKey, TValue> item)
        {
            lock (this)
            {
                return base.Remove(item);
            }
        }

        public override bool Remove(TKey key)
        {
            lock (this)
            {
                return base.Remove(key);
            }
        }

        public override bool TryGetValue(TKey key, out TValue value)
        {
            lock (this)
            {
                return base.TryGetValue(key, out value);
            }
        }

        public override ICollection<TValue> Values
        {
            get
            {
                lock (this)
                {
                    return base.Values;
                }
            }
        }

        public override TValue this[TKey key]
        {
            get
            {
                lock (this)
                {
                    return base[key];
                }
            }
            set
            {
                lock (this)
                {
                    base[key] = value;
                }
            }
        }
    }
}
