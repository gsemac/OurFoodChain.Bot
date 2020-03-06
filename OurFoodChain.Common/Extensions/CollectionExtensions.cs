﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OurFoodChain.Common.Extensions {

    public static class CollectionExtensions {

        public static void AddRange<T>(this ICollection<T> collection, IEnumerable<T> items) {

            foreach (T item in items)
                collection.Add(item);

        }
        public static bool RemoveAt<T>(this ICollection<T> collection, int index) {

            if (index < 0 || index >= collection.Count())
                throw new ArgumentOutOfRangeException(nameof(index));

            return collection.Remove(collection.ElementAt(index));

        }

        public static TValue GetOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key) {

            if (dictionary.TryGetValue(key, out TValue value))
                return value;

            return default(TValue);

        }
        public static TValue GetOrDefault<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> dictionary, TKey key) {

            if (dictionary.TryGetValue(key, out TValue value))
                return value;

            return default(TValue);

        }
        public static TValue GetOrDefault<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue) {

            if (dictionary.TryGetValue(key, out TValue value))
                return value;

            return defaultValue;

        }

    }

}