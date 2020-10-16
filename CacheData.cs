using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace DotStd
{
    /// <summary>
    /// Manage a process shared cache, prefix keys by type.
    /// needs NuGet for Caching Support.
    /// based on IMemoryCache for ASP .NET Core.
    /// like the OLD .NET MemoryCache.Default. 
    /// M$ discourages using multiple Caches for typed caches. but regions are not supported so delete by type is not supported natively.
    /// https://stackoverflow.com/questions/49176244/asp-net-core-clear-cache-from-imemorycache-set-by-set-method-of-cacheextensions/49425102#49425102
    /// Similar to https://github.com/alastairtree/LazyCache?WT.mc_id=-blog-scottha
    /// </summary>

    public static class CacheData
    {
        public const string kSep = "."; // name separator for grouping. similar to unsupported MemoryCache 'regions'
        public const char kSepChar = '.'; // name separator for grouping. similar to unsupported MemoryCache 'regions'

        public const int kSecond = 1;       // multiplier for seconds. for debug.

        private static IMemoryCache _memoryCache;       // my global/shared singleton instance of the Cache.
        private static SortedSet<string> _cacheKeys = new SortedSet<string>();   // dupe list of keys in _memoryCache. manual make thread safe.

        public static void Init(IMemoryCache memoryCache)
        {
            // set memoryCache = my global instance of the Cache.
            //  maybe Microsoft.Extensions.Caching.Memory.IMemoryCache else default.

            if (_memoryCache != null)
            {
                if (memoryCache == null)
                    return;
                // Is this intentional ?!
            }
            if (memoryCache == null)
            {
                // Make my own default MemoryCache.
                memoryCache = new MemoryCache(new MemoryCacheOptions());
            }
            _memoryCache = memoryCache;
        }

        public static ulong MakeHashCode(params object[] argsList)
        {
            // combine/composite args for a cache key. Assume the key has a type/group prefix.
            if (argsList == null)
                return 0;
            ulong hashCode = 0;
            foreach (object o in argsList)
            {
                hashCode = (hashCode << 13) | (hashCode >> (64 - 13)); // rotl
                hashCode ^= (ulong) o.GetHashCode();
            }
            return hashCode;
        }

        public static string MakeKeyArgs(params object[] argsList)
        {
            return MakeHashCode(argsList).ToString();
        }

        /// <summary>
        /// Helper to make a cache key.
        /// </summary>
        /// <param name="typePrefix">operation name followed by arguments to make it unique.</param>
        /// <returns>A string represents the query</returns>
        public static string MakeKey(string typePrefix, params object[] argsList)
        {
            // build the string representation of some expression for the cache key. (AKA cacheKey)
            return string.Concat(typePrefix, kSep, MakeKeyArgs(argsList));
        }

        private static void PostEvictionCallback(object cacheKey, object value, EvictionReason reason, object state)
        {
            // Memory cache uses this callback PostEvictionDelegate => its gone.
            if (reason != EvictionReason.Replaced)
            {
                lock (_cacheKeys)
                {
                    _cacheKeys.Remove(cacheKey.ToString());
                }
            }
        }

        /// <inheritdoc cref="IMemoryCache.TryGetValue"/>
        public static bool TryGetValue(string cacheKey, out object value)
        {
            if (_memoryCache == null)
            {
                value = null;
                return false;
            }
            return _memoryCache.TryGetValue(cacheKey, out value);
        }

        /// <summary>
        /// Find cacheKey in the cache, 
        /// </summary>
        /// <returns>The cacheKey object or null</returns>
        public static object Get(string cacheKey)
        {
            // try to get the query result from the cache
            if (!TryGetValue(cacheKey, out object value))
            {
                // Debug.Assert(!_cacheKeys.Contains(cacheKey));
                return null;
            }
            // Debug.Assert(_cacheKeys.Contains(cacheKey));
            return value;
        }

        /// <summary>
        /// Create or overwrite a empty/blank entry in the cache and add key to Dictionary. IMemoryCache
        /// </summary>
        /// <param name="cacheKey">An object identifying the entry.</param>
        /// <returns>The newly created <see cref="T:Microsoft.Extensions.Caching.Memory.ICacheEntry" /> instance.</returns>
        private static ICacheEntry CreateEntry(string cacheKey)
        {
            // Create empty value for key. Value filled in by caller. like IMemoryCache.
            // ASSUME lock (_cacheKeys)
            if (_memoryCache == null)
            {
                Init(null);
            }
            ICacheEntry entry = _memoryCache.CreateEntry(cacheKey);
            _cacheKeys.Add(cacheKey); // add or replace.
            entry.RegisterPostEvictionCallback(PostEvictionCallback);       // CacheEntryExtensions
            return entry;
        }

        /// <summary>
        /// Store/replace some object in the cache. Assume it isn't already here??
        /// </summary>
        public static void Set(string cacheKey, object value, int decaySec = 10 * kSecond)
        {
            // obj CANT be null, even though it might make sense.
            if (value == null)
                return;
            lock (_cacheKeys)
            {
                ICacheEntry entry = CreateEntry(cacheKey);
                entry.Value = value;
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(decaySec);
                entry.Dispose();    // This is what actually adds it to the cache. Weird.   
            }
        }

        public static T GetOrCreate<T>(string cacheKey, Func<string, T> factory)
        {
            lock (_cacheKeys)
            {
                if (!TryGetValue(cacheKey, out var value))
                {
                    // Debug.Assert(!_cacheKeys.Contains(cacheKey));
                    ICacheEntry entry = CreateEntry(cacheKey);
                    value = factory(cacheKey);
                    entry.Value = value;
                    entry.Dispose();    // This is what actually adds it to the cache. Weird.
                }
                else
                {
                    // Debug.Assert(_cacheKeys.Contains(cacheKey));
                }
                return (T)value;
            }
        }

        public static async Task<T> GetOrCreateAsync<T>(string cacheKey, Func<string, Task<T>> factory)
        {
            // async version of GetOrCreate

            if (!TryGetValue(cacheKey, out object value))
            {
                value = await factory(cacheKey);        // NOT thread locked. but safe-ish.
                lock (_cacheKeys)
                {
                    // Debug.Assert(!_cacheKeys.Contains(cacheKey));
                    ICacheEntry entry = CreateEntry(cacheKey);
                    entry.Value = value;
                    entry.Dispose();    // This is what actually adds it to the cache. Weird.
                }
            }
            else
            {
                // Debug.Assert(_cacheKeys.Contains(cacheKey));
            }
            return (T)value;
        }

        /// <inheritdoc cref="IMemoryCache.Remove"/>
        public static void ClearKey(string cacheKey)
        {
            // Force clear the cache for a single object. AKA Remove, ClearObj                
            if (_memoryCache == null)
                return;
            _memoryCache.Remove(cacheKey);  // This will call PostEvictionCallback().
        }

        public static void ClearAll()
        {
            lock (_cacheKeys)
            {
                var keys = _cacheKeys;  // must clone array as it is modified in callbacks to PostEvictionCallback().
                _cacheKeys = new SortedSet<string>();
                foreach (string cacheKey in keys)
                {
                    ClearKey(cacheKey);     // Will try to modify _cacheKeys
                }
            }
        }

        public static void ClearKeyPrefix(string cacheKeyPrefix)
        {
            // Force clear the cache for some objects of a type. (e.g. prefixed with key name)
            // https://stackoverflow.com/questions/9003656/memorycache-with-regions-support
            // BETTER ? https://stackoverflow.com/questions/4183270/how-to-clear-memorycache/22388943#22388943

            if (string.IsNullOrWhiteSpace(cacheKeyPrefix))
            {
                // all. like cache.Trim(100);
                ClearAll();
                return;
            }

            lock (_cacheKeys)
            {
                string cacheKeyMax = cacheKeyPrefix + '~';  // + ASCII value 126
                var keys = _cacheKeys.GetViewBetween(cacheKeyPrefix, cacheKeyMax).ToListDupe();  // get list that matches prefix. copied from _cacheKeys
                foreach (string cacheKey in keys)
                {
                    ClearKey(cacheKey);     // will modify _cacheKeys
                }
            }
        }
    }

    public static class CacheT<T> where T : class // : CacheData
    {
        // A Type specific variation of the global cache singleton CacheData.
        // Build on CacheData with Type

        private static string MakeKey(string id)
        {
            return string.Concat(typeof(T).Name, CacheData.kSep, id);
        }

        public static T Get(string id)
        {
            // Find the object in the cache if possible.
            if (id == null)       // shortcut.
                return null;
            ValidState.ThrowIfWhiteSpace(id, nameof(id));       // must have id. else use GetSingleton
            string cacheKey = MakeKey(id);
            object o = CacheData.Get(cacheKey);
            return (T)o;
        }
        public static T Get(int id)
        {
            ValidState.ThrowIfBadId(id, nameof(id));       // should check this before now.
            return Get(id.ToString());
        }

        public static T GetSingleton()
        {
            // Get singleton. Instance()
            return (T)CacheData.Get(typeof(T).Name);
        }

        public static void Set(string id, T obj, int decaySec = 60 * CacheData.kSecond)
        {
            string cacheKey = MakeKey(id);
            CacheData.Set(cacheKey, obj, decaySec);
        }
        public static void Set(int id, T obj, int decaySec = 60 * CacheData.kSecond)
        {
            Set(id.ToString(), obj, decaySec);
        }
        public static void SetSingleton(T obj, int decaySec = 60 * CacheData.kSecond)
        {
            CacheData.Set(typeof(T).Name, obj, decaySec);
        }

        public static void ClearKey(string id)
        {
            // Clear a single object by key.
            string cacheKey = MakeKey(id);
            CacheData.ClearKey(cacheKey);
        }
        public static void ClearKey(int id)
        {
            // Clear a single object by key.
            ClearKey(id.ToString());
        }
        public static void ClearSingleton()
        {
            CacheData.ClearKey(typeof(T).Name);
        }

        public static void ClearKeyPrefix(string cacheKeyPrefix = null)
        {
            // Clear this type or a sub set of this type.
            if (cacheKeyPrefix == null)
                cacheKeyPrefix = nameof(T);
            else if (!string.IsNullOrEmpty(cacheKeyPrefix))
                cacheKeyPrefix = MakeKey(cacheKeyPrefix);   // A weird sub-group ?
            CacheData.ClearKeyPrefix(cacheKeyPrefix);
        }
    }
}
