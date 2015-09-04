using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBlack.Caching.Timing;

namespace JetBlack.Caching.Collections.Generic
{
    public class TimeoutDictionary<TKey, TValue> : IDictionary<TKey, TValue>
    {
        private readonly IDictionary<TKey, TValue> _valueMap;
        private readonly IDictionary<TKey, DateTime> _timeMap;

        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly TimeSpan _timeout;

        public TimeoutDictionary(IDateTimeProvider dateTimeProvider, TimeSpan timeout)
        {
            _dateTimeProvider = dateTimeProvider;
            _timeout = timeout;
            _valueMap = new Dictionary<TKey, TValue>();
            _timeMap = new Dictionary<TKey, DateTime>();
        }

        private void Reap(DateTime now)
        {
            ISet<TKey> expiredKeys = new HashSet<TKey>();
            foreach (var item in _timeMap)
            {
                if (now - item.Value >= _timeout)
                    expiredKeys.Add(item.Key);
            }
            foreach (var key in expiredKeys)
            {
                _valueMap.Remove(key);
                _timeMap.Remove(key);
            }
        }

        public virtual IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            Reap(_dateTimeProvider.Now);
            return _valueMap.ToList().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            Add(item.Key, item.Value);
        }

        public virtual void Add(TKey key, TValue value)
        {
            var now = _dateTimeProvider.Now;
            DateTime time;
            if (!_timeMap.TryGetValue(key, out time))
            {
                _valueMap.Add(key, value);
                _timeMap.Add(key, now);
            }
            else if (now - time >= _timeout)
            {
                _valueMap[key] = value;
                _timeMap[key] = now;
            }
            else
                throw new ArgumentException("An element with the same key already exists", "key");
        }

        public virtual void Clear()
        {
            _valueMap.Clear();
            _timeMap.Clear();
        }

        public virtual bool Contains(KeyValuePair<TKey, TValue> item)
        {
            DateTime time;
            if (!_timeMap.TryGetValue(item.Key, out time))
                return false;

            if (_dateTimeProvider.Now - time < _timeout)
                return Equals(_valueMap[item.Key], item.Value);

            _valueMap.Remove(item.Key);
            _timeMap.Remove(item.Key);

            return false;
        }

        public virtual void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            Reap(_dateTimeProvider.Now);
            foreach (var item in _valueMap)
                array[arrayIndex++] = item;
        }

        public virtual bool Remove(KeyValuePair<TKey, TValue> item)
        {
            DateTime time;
            if (!_timeMap.TryGetValue(item.Key, out time))
                return false;

            var isStale = _dateTimeProvider.Now - time >= _timeout;
            var isEqual = Equals(_valueMap[item.Key], item.Value);
            if (isStale || isEqual)
            {
                _timeMap.Remove(item.Key);
                _valueMap.Remove(item.Key);
            }

            return isEqual && !isStale;
        }

        public virtual bool Remove(TKey key)
        {
            DateTime time;
            if (!_timeMap.TryGetValue(key, out time))
                return false;

            _timeMap.Remove(key);
            _valueMap.Remove(key);

            return _dateTimeProvider.Now - time >= _timeout;
        }

        public virtual int Count
        {
            get
            {
                Reap(_dateTimeProvider.Now);
                return _valueMap.Count;
            }
        }

        public bool IsReadOnly { get { return false; } }

        public virtual bool ContainsKey(TKey key)
        {
            DateTime time;
            if (!_timeMap.TryGetValue(key, out time))
                return false;

            if (_dateTimeProvider.Now - time < _timeout)
                return true;

            _valueMap.Remove(key);
            _timeMap.Remove(key);

            return false;
        }

        public virtual bool TryGetValue(TKey key, out TValue value)
        {
            DateTime time;
            if (_timeMap.TryGetValue(key, out time) && _dateTimeProvider.Now - time < _timeout)
                return _valueMap.TryGetValue(key, out value);

            value = default(TValue);
            return false;
        }

        public virtual TValue this[TKey key]
        {
            get
            {
                DateTime time;
                if (_timeMap.TryGetValue(key, out time))
                {
                    if (_dateTimeProvider.Now - time < _timeout)
                        return _valueMap[key];

                    _valueMap.Remove(key);
                    _timeMap.Remove(key);
                }

                throw new KeyNotFoundException();
            }
            set
            {
                _valueMap[key] = value;
                _timeMap[key] = _dateTimeProvider.Now;
            }
        }

        public virtual ICollection<TKey> Keys
        {
            get
            {
                Reap(_dateTimeProvider.Now);
                return _valueMap.Keys;
            }
        }

        public virtual ICollection<TValue> Values
        {
            get
            {
                Reap(_dateTimeProvider.Now);
                return _valueMap.Values;
            }
        }
    }
}
