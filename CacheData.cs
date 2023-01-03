using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace DotStd
{
    /// <summary>
    /// Manage a process shared (Singleton) cache, prefix keys by type.
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

        private static IMemoryCache? _memoryCache;       // my global/shared singleton instance of the Cache.
        private static SortedSet<string> _cacheKeys = new SortedSet<string>();   // dupe list of keys in _memoryCache. manual make thread safe.

        public static void Init(IMemoryCache? memoryCache)
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

        /// <summary>
        /// combine/composite args for a cache key. Assume the key has a type/group prefix.
        /// </summary>
        /// <param name="argsList"></param>
        /// <returns></returns>
        public static ulong MakeHashCode(params object[] argsList)
        {
            if (argsList == null)
                return 0;
            ulong hashCode = 0;
            foreach (object o in argsList)
            {
                hashCode = (hashCode << 13) | (hashCode >> (64 - 13)); // rotl
                hashCode ^= (ulong)o.GetHashCode();
            }
            return hashCode;
        }

        public static string MakeKeyArgsHash(params object[] argsList)
        {
            // Make hashcode from args.
            return MakeHashCode(argsList).ToString();
        }

        /// <summary>
        /// Memory cache uses this callback PostEvictionDelegate => its gone.
        /// </summary>
        /// <param name="cacheKey">might be string or int ?</param>
        /// <param name="value"></param>
        /// <param name="reason"></param>
        /// <param name="state"></param>
        private static void PostEvictionCallback(object cacheKey, object value, EvictionReason reason, object state)
        {
            if (reason != EvictionReason.Replaced)
            {
                lock (_cacheKeys)
                {
                    _cacheKeys.Remove(cacheKey.ToString() ?? string.Empty);
                }
            }
        }

        /// <inheritdoc cref="IMemoryCache.TryGetValue"/>
        public static bool TryGetValue(string cacheKey, [NotNullWhen(true)] out object? value)
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
        public static object? Get(string cacheKey)
        {
            // try to get the query result from the cache
            if (!TryGetValue(cacheKey, out object? value))
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
                ValidState.ThrowIfNull(_memoryCache, nameof(_memoryCache));
            }
            ICacheEntry entry = _memoryCache.CreateEntry(cacheKey);
            _cacheKeys.Add(cacheKey); // add or replace.
            entry.RegisterPostEvictionCallback(PostEvictionCallback);       // CacheEntryExtensions
            return entry;
        }

        /// <summary>
        /// Store/replace some object in the cache. Assume it isn't already here??
        /// </summary>
        public static void Set(string cacheKey, object? value, int decaySec = 10 * kSecond)
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
                if (!TryGetValue(cacheKey, out object? value))
                {
                    // Debug.Assert(!_cacheKeys.Contains(cacheKey));
                    ICacheEntry entry = CreateEntry(cacheKey);
                    T valueNew = factory(cacheKey);
                    entry.Value = valueNew;
                    entry.Dispose();    // This is what actually adds it to the cache. Weird.
                    return valueNew;
                }

                // Debug.Assert(_cacheKeys.Contains(cacheKey));
                return (T)value;
            }
        }

        public static async Task<T> GetOrCreateAsync<T>(string cacheKey, Func<string, Task<T>> factory)
        {
            // async version of GetOrCreate

            if (!TryGetValue(cacheKey, out object? value))
            {
                T valueNew = await factory(cacheKey);        // NOT thread locked. but safe-ish.
                lock (_cacheKeys)
                {
                    // Debug.Assert(!_cacheKeys.Contains(cacheKey));
                    ICacheEntry entry = CreateEntry(cacheKey);
                    entry.Value = valueNew;
                    entry.Dispose();    // This is what actually adds it to the cache. Weird.
                    return valueNew;
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
            // Force clear the cache for some objects of a type. (e.g. prefixed with key type name)
            // https://stackoverflow.com/questions/9003656/memorycache-with-regions-support
            // BETTER ? https://stackoverflow.com/questions/4183270/how-to-clear-memorycache/22388943#22388943
            // Not to be used with MakeHashCode()/MakeKeyArgsHash()

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

    /// <summary>
    /// A cache with a unique type prefix name.
    /// </summary>
    /// <typeparam name="T"></typeparam>    
    public class CacheNameT<T> where T : class // : CacheData
    {
        protected readonly string _TypePrefix;     // unique type prefix. manually set or typeof(T).Name.

        public CacheNameT(string typePrefix)
        {
            _TypePrefix = typePrefix;
        }
        public CacheNameT()
        {
            // just use the type name.
            _TypePrefix = typeof(T).Name;
        }
    }

    /// <summary>
    /// Cache a singleton based on type. _TN = typeof(T).Name
    /// </summar>
    public class CacheSingleT<T> : CacheNameT<T> where T : class
    {
        public T? Get()
        {
            // Get singleton for type T. Instance().
            // null = may need to lazy load it.
            return (T?)CacheData.Get(_TypePrefix);
        }
        public void Set(T obj, int decaySec = 60 * CacheData.kSecond)
        {
            CacheData.Set(_TypePrefix, obj, decaySec);
        }
        public void Clear()
        {
            CacheData.ClearKey(_TypePrefix);
        }
        public CacheSingleT(string typePrefix) : base(typePrefix)
        {
        }
        public CacheSingleT() : base()
        {
        }
    }

    /// <summary>
    /// Cache objects of type T where the key is an int.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class CacheIntT<T> : CacheNameT<T> where T : class // : CacheData
    {
        private string MakeKey(int id)
        {
            // CacheData::MakeKey
            return string.Concat(_TypePrefix, CacheData.kSep, id);
        }
        public T? Get(int id)
        {
            ValidState.ThrowIfBadId(id, nameof(id));       // should check this before now.
            string cacheKey = MakeKey(id);
            object? o = CacheData.Get(cacheKey);
            return (T?)o;
        }
        public void Set(int id, T? obj, int decaySec = 60 * CacheData.kSecond)
        {
            if (obj == null)
                return;
            string cacheKey = MakeKey(id);
            CacheData.Set(cacheKey, obj, decaySec);
        }
        public void ClearKey(int id)
        {
            // Clear a single object by key.
            string cacheKey = MakeKey(id);
            CacheData.ClearKey(cacheKey);
        }
        public CacheIntT(string typePrefix) : base(typePrefix)
        {
        }
        public CacheIntT()
        {
        }
    }

    /// <summary>
    /// A Type specific variation of the global cache. 
    /// Build on CacheData with Type Id as complex string
    /// </summary>
    /// <typeparam name="T">object being cached</typeparam>
    public class CacheStrT<T> : CacheNameT<T> where T : class // : CacheData
    {
        private string MakeKey(string keyArg)
        {
            // CacheData::MakeKey
            return string.Concat(_TypePrefix, CacheData.kSep, keyArg);
        }

        public T? Get(string? keyArg)
        {
            // Find the object in the cache if possible. id is a unique string.
            if (keyArg == null)       // shortcut null check.
                return null;
            ValidState.ThrowIfWhiteSpace(keyArg, nameof(keyArg));       // must have id. else use CacheSingleT
            string cacheKey = MakeKey(keyArg);
            object? o = CacheData.Get(cacheKey);
            return (T?)o;
        }

        public void Set(string keyArg, T obj, int decaySec = 60 * CacheData.kSecond)
        {
            string cacheKey = MakeKey(keyArg);
            CacheData.Set(cacheKey, obj, decaySec);
        }

        public void ClearKey(string keyArg)
        {
            // Clear a single object by key.
            string cacheKey = MakeKey(keyArg);
            CacheData.ClearKey(cacheKey);
        }

        public void ClearKeyPrefix(string? cacheKeyPrefix = null)
        {
            // Clear this type or a sub set of this type.
            if (cacheKeyPrefix == null)
                cacheKeyPrefix = typeof(T).Name;
            else if (!string.IsNullOrEmpty(cacheKeyPrefix))
                cacheKeyPrefix = MakeKey(cacheKeyPrefix);   // A weird sub-group ?
            CacheData.ClearKeyPrefix(cacheKeyPrefix);
        }

        public CacheStrT(string typePrefix) : base(typePrefix)
        {
        }
        public CacheStrT()
        {
        }
    }
}
