using System.Collections.Generic;

namespace JetBlack.Caching.Collections.Generic
{
    /// <summary>
    /// A helper extension for creating KeyValuePair&lt;TKey, TValue&gt;.
    /// </summary>
    public static class KeyValuePair
    {
        /// <summary>
        /// Creates a key value pair.
        /// </summary>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <returns>A KeyValuePair of the given types, key, and value.</returns>
        public static KeyValuePair<TKey, TValue> Create<TKey, TValue>(TKey key, TValue value)
        {
            return new KeyValuePair<TKey, TValue>(key, value);
        }
    }
}
