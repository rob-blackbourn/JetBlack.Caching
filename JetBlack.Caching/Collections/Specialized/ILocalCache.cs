using System.Collections.Generic;

namespace JetBlack.Caching.Collections.Specialized
{
    /// <summary>
    /// The local cache follows a subset of the usual dictionary interface
    /// with the exception of the add method, which must return an item
    /// that was ovewritten .
    /// </summary>
    /// <typeparam name="TKey">The type of the keys.</typeparam>
    /// <typeparam name="TValue">The type of the values.</typeparam>
    public interface ILocalCache<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>
    {
        /// <summary>
        /// Clear the cache.
        /// </summary>
        void Clear();

        /// <summary>
        /// Copy the cache to an array, starting at a given index.
        /// </summary>
        /// <param name="array">The destination array.</param>
        /// <param name="arrayIndex">The point in the destination array at which copying should start.</param>
        void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex);

        /// <summary>
        /// The number of items in the cache.
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Determines whether a key is present in the cache.
        /// </summary>
        /// <param name="key">The key to find.</param>
        /// <returns>If the key was foud true, otherwise false.</returns>
        bool ContainsKey(TKey key);

        /// <summary>
        /// Adds a new key to the cache. which may result in overwriting an existing key.
        /// </summary>
        /// <param name="key">The key to add.</param>
        /// <param name="value">The value associated to the key.</param>
        /// <param name="overwritten">If the cache was already full, the value of the item that was overwritten.</param>
        /// <returns>If an item was overwritten true, otherwise false.</returns>
        bool AddOrOverwrite(TKey key, TValue value, out KeyValuePair<TKey,TValue> overwritten);

        /// <summary>
        /// Remove an item from the cache.
        /// </summary>
        /// <param name="key">The key for the item.</param>
        /// <returns>If the item was found true, otherwise false.</returns>
        bool Remove(TKey key);

        /// <summary>
        /// Try to get a vlue from the cahe.
        /// </summary>
        /// <param name="key">The key to find.</param>
        /// <param name="value">If the key was found, its value.</param>
        /// <returns>If the key was found true, otherwise false.</returns>
        bool TryGetValue(TKey key, out TValue value);

        /// <summary>
        /// Indexes the cache by a given key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>The value associated to the key.</returns>
        TValue this[TKey key] { get; set; }

        /// <summary>
        /// The keys in the cache.
        /// </summary>
        ICollection<TKey> Keys { get; }

        /// <summary>
        /// The values in the cache.
        /// </summary>
        ICollection<TValue> Values { get; }
    }
}