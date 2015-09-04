using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBlack.Caching.Timing;

namespace JetBlack.Caching.Collections.Generic
{
    /// <summary>
    /// A dictionary in which the values are considered not to exist after a given time period has elapsed.
    /// </summary>
    /// <typeparam name="TKey">The type of the keys.</typeparam>
    /// <typeparam name="TValue">The type of the values.</typeparam>
    public class TimeoutDictionary<TKey, TValue> : IDictionary<TKey, TValue>
    {
        private readonly IDictionary<TKey, TValue> _valueMap;
        private readonly IDictionary<TKey, DateTime> _timeMap;

        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly TimeSpan _timeout;

        /// <summary>
        /// Constructs a new timeout dictionary with a given timeout and optionally a date time provider.
        /// </summary>
        /// <param name="timeout">The tiem after which a key is no longer valid.</param>
        /// <param name="dateTimeProvider">An optional date time provider.</param>
        public TimeoutDictionary(TimeSpan timeout, IDateTimeProvider dateTimeProvider = null)
        {
            _dateTimeProvider = dateTimeProvider ?? new DateTimeProvider();
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

        /// <summary>
        /// Gets an enumerator for the collection.
        /// </summary>
        /// <returns>An enumerator of key value pairs.</returns>
        public virtual IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            Reap(_dateTimeProvider.Now);
            return _valueMap.ToList().GetEnumerator();
        }

        /// <summary>
        /// Gets an untyped anumerator for the collection.
        /// </summary>
        /// <returns>An untyped enumerator.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Adds an item to the collection.
        /// </summary>
        /// <param name="item">The item to add.</param>
        public void Add(KeyValuePair<TKey, TValue> item)
        {
            Add(item.Key, item.Value);
        }

        /// <summary>
        /// Adds an item to the collection.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <exception cref="ArgumentException">Thrown if the key already exists.</exception>
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

        /// <summary>
        /// Removes all items from the collection.
        /// </summary>
        public virtual void Clear()
        {
            _valueMap.Clear();
            _timeMap.Clear();
        }

        /// <summary>
        /// Determines if the item exists in the collection.
        /// </summary>
        /// <param name="item">The item to look for.</param>
        /// <returns>If the item exists then true, otherwise false.</returns>
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

        /// <summary>
        /// Copies the collection to an array.
        /// </summary>
        /// <param name="array">The destination array.</param>
        /// <param name="arrayIndex">The point in the destination array at which the copying starts.</param>
        public virtual void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            Reap(_dateTimeProvider.Now);
            foreach (var item in _valueMap)
                array[arrayIndex++] = item;
        }

        /// <summary>
        /// Remove an item from the collection.
        /// </summary>
        /// <param name="item">The item to be removed.</param>
        /// <returns>If the item was remove true, otherwise false.</returns>
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

        /// <summary>
        /// Removes an item with the given key from the collection.
        /// </summary>
        /// <param name="key">The key of the item to be removed.</param>
        /// <returns>If the item was removed true, otherwise false.</returns>
        public virtual bool Remove(TKey key)
        {
            DateTime time;
            if (!_timeMap.TryGetValue(key, out time))
                return false;

            _timeMap.Remove(key);
            _valueMap.Remove(key);

            return _dateTimeProvider.Now - time >= _timeout;
        }

        /// <summary>
        /// The number of items in the collection.
        /// </summary>
        public virtual int Count
        {
            get
            {
                Reap(_dateTimeProvider.Now);
                return _valueMap.Count;
            }
        }

        /// <summary>
        /// Always false.
        /// </summary>
        public bool IsReadOnly { get { return false; } }

        /// <summary>
        /// Determines if a key exists in the collection.
        /// </summary>
        /// <param name="key">The key to find.</param>
        /// <returns>If the key was found true, otherwise false.</returns>
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

        /// <summary>
        /// Try to get an item with a given key and provide feedback.
        /// </summary>
        /// <param name="key">The key to find.</param>
        /// <param name="value">The value set if the key was found.</param>
        /// <returns>If the key was found true, otherwise false.</returns>
        public virtual bool TryGetValue(TKey key, out TValue value)
        {
            DateTime time;
            if (_timeMap.TryGetValue(key, out time) && _dateTimeProvider.Now - time < _timeout)
                return _valueMap.TryGetValue(key, out value);

            value = default(TValue);
            return false;
        }

        /// <summary>
        /// Gets or sets the value for a given key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>The value of the given key.</returns>
        /// <exception cref="KeyNotFoundException">Thrown if the key was not found.</exception>
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

        /// <summary>
        /// The keys in the collection.
        /// </summary>
        public virtual ICollection<TKey> Keys
        {
            get
            {
                Reap(_dateTimeProvider.Now);
                return _valueMap.Keys;
            }
        }

        /// <summary>
        /// The values in the collection.
        /// </summary>
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
