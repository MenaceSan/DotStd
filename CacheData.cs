using System;
using System.Runtime.Caching;           // 4.6 ObjectCache, CacheItemPolicy

namespace DotStd
{
    /// <summary>
    /// in memory cache for query results (or any other expensive data) to avoid repeated round trips to db for mostly static data.
    /// NOTE .NET Core and Std need NuGet for Caching Support.
    /// </summary>
    public static class CacheData
    {
        public const string kSep = ".";

        /// <summary>
        /// Gets a cache key for a query result object.
        /// </summary>
        /// <param name="list">operation name followed by arguments to make it unique.</param>
        /// <returns>A string represents the query</returns>
        public static string MakeKey(string type, params string[] argsList)
        {
            // build the string representation of some expression for the cache key. (AKA cacheKey)

            string KeyArgs = string.Join(kSep, argsList);
            if (KeyArgs.Length > 16)    // Just hash it if it seems too large.
                KeyArgs = KeyArgs.GetHashCode().ToString();

            return string.Concat(type, kSep, KeyArgs);    // ??? reverse this string to make it more evenly distributed ?
        }

        public static void ClearObj(string cacheKey)
        {
            // Force clear the cache for some object.
            var cache = MemoryCache.Default;
            cache.Remove(cacheKey);
        }

        public static void FlushObjs(string cacheKeyPrefix)
        {
            // Force clear the cache for some objects.
            var cache = MemoryCache.Default;
            cache.Remove(cacheKeyPrefix);
            // TODO allow type prefix.
        }

        /// <summary>
        /// Find cacheKey in the cache, 
        /// </summary>
        /// <returns>The cacheKey object or null</returns>
        public static object Get(string cacheKey)
        {
            // try to get the query result from the cache
            var cache = MemoryCache.Default;
            return cache.Get(cacheKey);
        }

        /// <summary>
        /// Store some object in the cache. Assume it isn't already here??
        /// </summary>
        public static void Set(string cacheKey, object obj, int decaysec = 10)
        {
            // obj cant be null, even though it might make sense.
            if (obj == null)
                return;
            var cache = MemoryCache.Default;
            var policy = new CacheItemPolicy() { AbsoluteExpiration = DateTime.Now.AddSeconds(decaysec) };
            // var policy = new CacheItemPolicy() { SlidingExpiration = TimeSpan.FromSeconds(decaysec) };
            cache.Set(cacheKey, obj, policy);
        }
    }

    public static class CacheObj<T> where T : class // : CacheData
    {
        // Build on CacheData/MemoryCache.Default with Type

        public static T Get(string id)
        {
            // Find the object in the cache if possible.
            string cacheKey = string.Concat(typeof(T).Name, CacheData.kSep, id);
            return (T)CacheData.Get(cacheKey);
        }
        public static T Get(int id)
        {
            ValidState.ThrowIfBadId(id, nameof(id));       // should check this before now.
            return Get(id.ToString());
        }

        public static void Set(string id, T obj, int decaysec)
        {
            string cacheKey = string.Concat(typeof(T).Name, CacheData.kSep, id);
            CacheData.Set(cacheKey, obj, decaysec);
        }
        public static void Set(int id, T obj, int seconds)
        {
            Set(id.ToString(), obj, seconds);
        }

        public static void ClearObj(string id)
        {
            string cacheKey = string.Concat(typeof(T).Name, CacheData.kSep, id);
            CacheData.ClearObj(cacheKey);
        }
        public static void ClearObj(int id)
        {
            ClearObj(id.ToString());
        }

    }
}
