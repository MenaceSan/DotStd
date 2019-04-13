using System;
using System.Collections.Generic;
using System.Runtime.Caching;
using System.Threading.Tasks;

namespace DotStd
{
    public static class CacheLazy<T> where T : class
    {
        // Build on MemoryCache.Default with Type. like CacheObj<T>
        // Maybe rename this to "CacheUnique" ??
        // This is a cache like CacheData that also has a weak reference to tell if the object might still be referenced some place.
        // The data stays around for cache time with the hard reference.
        // The data also has a weak reference count which makes sure the object is the same for all callers. 
        // All users get the SAME version of the object! There is only one version of an object with the same 'Id'. It can be updated dynamically for all consumers.
        // Similar to https://github.com/alastairtree/LazyCache?WT.mc_id=-blog-scottha

        public static Dictionary<string, WeakReference> _WeakRefs = new Dictionary<string, WeakReference>();  // use this to tell if the object might still be referenced by someone even though the cache has aged out.
        public static DateTime _LastFlush = DateTime.MinValue;      // Flush dead stuff out of the _WeakRefs cache eventually.

        public static void FlushDead()
        {
            // Periodically flush the disposed weak refs.
            // Throttle calling this using FlushDeadTick()

            lock (_WeakRefs)
            {
                var removals = new List<string>();
                foreach (var w in _WeakRefs)
                {
                    if (!w.Value.IsAlive)
                    {
                        // We know/assume it is not in the hard / timed cache.
                        // Get rid of it.
                        removals.Add(w.Key);
                    }
                }
                foreach (string key in removals)
                {
                    _WeakRefs.Remove(key);
                }
            }
        }

        public static void FlushDeadTick()
        {
            // Make sure we dont call FlushDead too often.
            DateTime now = DateTime.UtcNow;
            if ((now - _LastFlush).TotalMinutes < 2)    // throttle.
                return;
            _LastFlush = now;
            FlushDead();
        }

        public static T GetT<TId>(TId id, int decaysec, Func<TId, T> loader)
        {
            // NOTE: await Task<T> cant be done inside lock !???
            // like IMemoryCache GetOrCreate

            var cache = MemoryCache.Default;
            string cacheKey = string.Concat(typeof(T).Name, CacheData.kSep, id);

            lock (_WeakRefs)
            {
                // Find in the hard ref cache first.
                T obj = (T)cache.Get(cacheKey);
                if (obj != null)
                {
                    return obj;  // got it.
                }

                // Fallback to the weak ref cache next. e.g. someone has a ref that existed longer than the cache. get it.
                WeakReference wr;
                if (_WeakRefs.TryGetValue(cacheKey, out wr))
                {
                    if (wr.IsAlive)
                    {
                        // someone still has a ref to this . so continue to use it. we should reload this from the db though.
                        obj = (T)wr.Target;
                    }
                }

                // lastly load it if we cant find it otherwise.
                if (loader != null)
                {
                    // NOTE: await Task<T> cant be done inside lock !
                    T objLoad = loader.Invoke(id);
                    ValidState.ThrowIfNull(objLoad, nameof(objLoad));

                    if (obj == null)
                    {
                        obj = objLoad;  // Fresh load.
                        if (wr != null)
                        {
                            _WeakRefs.Remove(cacheKey);
                        }
                        _WeakRefs.Add(cacheKey, new WeakReference(obj));
                    }
                    else
                    {
                        PropertyUtil.InjectProperties<T>(obj, objLoad); // refresh props in old weak ref.
                    }
                }

                if (obj != null)
                {
                    // store it in cache.
                    var policy = new CacheItemPolicy() { AbsoluteExpiration = DateTime.UtcNow.AddSeconds(decaysec) };     // We have already outlived our time. we should refresh this !
                    cache.Set(cacheKey, obj, policy);
                }

                return obj;
            }
        }

        public static T Get(int id, int decaysec, Func<int, T> loader)
        {
            // Find this object in the cache. Load it if it isn't present.
            // T loader(int)
            // like IMemoryCache GetOrCreate

            if (!ValidState.IsValidId(id))  // can't do anything about this. always null.
                return null;

            return GetT(id, decaysec, loader);
        }

        public static void ClearObjT<TId>(TId id, T newValues)
        {
            // Object has changed. So sync up all other users with the new values!
            // id is not int ?
            lock (_WeakRefs)
            {
                T obj = GetT(id, 10, null);
                if (obj == null)
                    return;     // Its not loaded so do nothing.
                PropertyUtil.InjectProperties<T>(obj, newValues); // refresh props in old weak ref.
            }
        }

        public static void ClearObj(int id, T newValues)
        {
            if (!ValidState.IsValidId(id))  // can't do anything about this. always null.
                return;
            ClearObjT(id, newValues);
        }

#if false
        public static async Task<T> GetAsync(int id, int decaysec, Task<T> myTask)
        {
            // TODO FIXME
            // NOTE: await Task<T> cant be done inside lock !
            var myFun = async () => await myTask;
            return Get(id, decaysec, myFun);
        }
#endif

    }
}
