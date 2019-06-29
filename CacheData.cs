using System;
using System.Runtime.Caching;
// using System.Runtime.Caching;           // 4.6 ObjectCache, CacheItemPolicy

namespace DotStd
{
    /// <summary>
    /// Wrapper/Helper for the .NET MemoryCache.Default. Has a single namespace that all share, so prefix keys by type.
    /// M$ discourages using multiple Caches for typed caches. but regions are not supported so delete by type is not supported natively.
    /// NOTE .NET Core and Std need NuGet for Caching Support.
    /// similar to IMemoryCache for ASP core.
    /// </summary>
    public static class CacheData
    {
        public const string kSep = "."; // name separator for grouping. similar to unsupported 'regions'

        public static string MakeKeyArgs(params object[] argsList)
        {
            // composite args for the key.
            string keyArgs = string.Join(kSep, argsList);
            if (keyArgs.Length > 16)    // Just hash it if it seems too large. DANGER ??
                keyArgs = keyArgs.GetHashCode().ToString();
            return keyArgs;
        }

        /// <summary>
        /// Helper to make a cache key.
        /// </summary>
        /// <param name="list">operation name followed by arguments to make it unique.</param>
        /// <returns>A string represents the query</returns>
        public static string MakeKey(string type, params object[] argsList)
        {
            // build the string representation of some expression for the cache key. (AKA cacheKey)
            return string.Concat(type, kSep, MakeKeyArgs(argsList));    // ??? reverse this string to make hash more evenly distributed ?
        }

        public static void ClearObj(string cacheKey)
        {
            // Force clear the cache for a single object.
            var cache = MemoryCache.Default;
            cache.Remove(cacheKey);
        }

        public static void ClearType(string cacheKeyPrefix)
        {
            // This is VERY NOT efficient !!
            // Force clear the cache for some objects of a type. (e.g. prefixed with key name)
            // https://stackoverflow.com/questions/9003656/memorycache-with-regions-support
            // BETTER ? https://stackoverflow.com/questions/4183270/how-to-clear-memorycache/22388943#22388943

            var cache = MemoryCache.Default;
            if (cacheKeyPrefix == "")
            {
                // all.
                cache.Trim(100);
                return;
            }

            foreach (var item in cache)
            {
                if (item.Key.StartsWith(cacheKeyPrefix))
                    cache.Remove(item.Key);
            }
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

        // SetSlide()

        /// <summary>
        /// Store some object in the cache. Assume it isn't already here??
        /// </summary>
        public static void Set(string cacheKey, object obj, int decaysec = 10)
        {
            // obj cant be null, even though it might make sense.
            // NOTE: cache time is Utc time. 

            if (obj == null)
                return;
            var cache = MemoryCache.Default;
            var policy = new CacheItemPolicy() { AbsoluteExpiration = DateTime.UtcNow.AddSeconds(decaysec) };
            // var policy = new CacheItemPolicy() { SlidingExpiration = TimeSpan.FromSeconds(decaysec) };
            cache.Set(cacheKey, obj, policy);
        }
    }

    public static class CacheObj<T> where T : class // : CacheData
    {
        // Build on CacheData/MemoryCache.Default with Type

        public static string MakeKey(string id)
        {
            return string.Concat(typeof(T).Name, CacheData.kSep, id);
        }

        public static T Get(string id)
        {
            // Find the object in the cache if possible.
            string cacheKey = MakeKey(id);
            object o = CacheData.Get(cacheKey);
            return (T)o;
        }
        public static T Get(int id)
        {
            ValidState.ThrowIfBadId(id, nameof(id));       // should check this before now.
            return Get(id.ToString());
        }

        public static void Set(string id, T obj, int decaysec = 60)
        {
            string cacheKey = MakeKey(id);
            CacheData.Set(cacheKey, obj, decaysec);
        }
        public static void Set(int id, T obj, int decaysec = 60)
        {
            Set(id.ToString(), obj, decaysec);
        }

        public static void ClearObj(string id)
        {
            // Clear a single object by key.
            string cacheKey = MakeKey(id);
            CacheData.ClearObj(cacheKey);
        }
        public static void ClearObj(int id)
        {
            // Clear a single object by key.
            ClearObj(id.ToString());
        }
        public static void ClearType(string cacheKeyPrefix = null)
        {
            // Clear this type or a sub set of this type.
            if (cacheKeyPrefix == null)
                cacheKeyPrefix = nameof(T);
            else if (cacheKeyPrefix != "")
                cacheKeyPrefix = MakeKey(cacheKeyPrefix);
            CacheData.ClearType(cacheKeyPrefix);
        }
    }
}
