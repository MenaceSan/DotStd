using System;
using System.Collections.Generic;

namespace DotStd
{
    public static class CacheMultiton<T> where T : class
    {
        // Build on CacheT<T> for Multiton pattern
        // https://en.wikipedia.org/wiki/Multiton_pattern
        // All users get the SAME version of the object! There is only one version of an object with the same 'Id'. It can be updated dynamically for all consumers.
        // This is a cache like CacheData that also has a weak reference to tell if the object might still be referenced some place.
        // 1. The data stays around for cache time with the hard reference.
        // 2. The data also has a weak reference count which makes sure the object is the same for all callers. 

        public static Dictionary<string, WeakReference> _WeakRefs = new Dictionary<string, WeakReference>();  // use this to tell if the object might still be referenced by someone even though the cache has aged out.
        public static DateTime _LastFlushTime = DateTime.MinValue;      // Flush dead stuff out of the _WeakRefs cache eventually.

        internal static void FlushDead()
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

        public static void FlushDeadTick(DateTime utcNow)
        {
            // Make sure we don't call FlushDead too often. throttle
            if ((utcNow - _LastFlushTime).TotalMinutes < 2)    // throttle.
                return;
            _LastFlushTime = utcNow;
            FlushDead();
        }

        public static T GetKeyT<TId>(TId id, int decaysec, Func<TId, T> factory)
        {
            // Get object from cache and Load/Create it if needed.
            // NOTE: await Task<T> cant be done inside lock !???
            // TId = int or string.
            // like IMemoryCache GetOrCreate

            string cacheKey = string.Concat(typeof(T).Name, CacheData.kSep, id);

            lock (_WeakRefs)
            {
                // Find in the hard ref cache first.
                T obj = (T)CacheData.Get(cacheKey);
                if (obj != null)
                {
                    return obj;  // got it.
                }

                // Fallback to the weak ref cache next. e.g. someone has a ref that existed longer than the cache? get it.
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
                if (factory != null)
                {
                    // NOTE: await Task<T> cant be done inside lock !
                    T objLoad = factory.Invoke(id);
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
                    // store it in cache. refresh decaySec.
                    CacheData.Set(cacheKey, obj, decaysec);
                }

                return obj;
            }
        }

        public static T Get(int id, int decaysec, Func<int, T> factory)
        {
            // Find this object in the cache. Load/Create it if it isn't present.
            // T loader(int)
            // like IMemoryCache GetOrCreate

            if (!ValidState.IsValidId(id))  // can't do anything about this. always null.
                return null;

            return GetKeyT(id, decaysec, factory);
        }

        public static void UpdateObject<TId>(TId id, T newValues)
        {
            // Object has changed. So sync up any other users with the new values!
            // id is not int ?
            lock (_WeakRefs)
            {
                T obj = GetKeyT(id, 10, null);
                if (obj == null)
                    return;     // Its not loaded so do nothing.
                PropertyUtil.InjectProperties<T>(obj, newValues); // refresh props in old weak ref.
            }
        }

        public static void UpdateObject(int id, T newValues)
        {
            if (!ValidState.IsValidId(id))  // can't do anything about this. always null.
                return;
            UpdateObject(id, newValues);
        }

#if false
        public static async Task<T> GetAsync(int id, int decaysec, Func<int, Task<T>>  factory)
        {
            // TODO FIXME make async version.
            // NOTE: await Task<T> cant be done inside lock !
            var factory2 = async () => await factory;
            return Get(id, decaysec, factory2);
        }
#endif

    }
}
