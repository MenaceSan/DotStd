using System;
using System.Diagnostics.CodeAnalysis;

namespace DotStd
{
    /// <summary>
    /// Manage a singleton of a type. Allow late creation. NOT the same as Auto LazyLoad
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Singleton<T>
    {
        static T? _Instance;  // singleton.

        /// <summary>
        /// For debug/test. allow singleton replacement.
        /// </summary>
        /// <param name="i"></param>
        public static void InitInstanceTest(T i)
        {
            _Instance = i;
        }

        public static T InitInstance(T i)
        {
            ValidState.ThrowIf(_Instance != null, nameof(_Instance));   // dont set it twice!
            _Instance = i;
            return i;
        }

        /// <summary>
        /// Has it been created yet?
        /// </summary>
        /// <returns></returns>
        [MemberNotNullWhen(returnValue: true, member: nameof(_Instance))]
        public static bool IsInit()
        {
            return _Instance != null;
        }

        /// <summary>
        /// Must be created by InitInstance call.
        /// </summary>
        /// <returns></returns>
        [MemberNotNull(nameof(_Instance))]
        public static T Instance()
        {            
            return ValidState.GetNotNull(_Instance, nameof(_Instance)); ;
        }

        [MemberNotNull(nameof(_Instance))]
        public static T Instance(Func<T> creator)
        {
            if (_Instance == null) { _Instance = creator(); }   // NOT thread safe !!
            return Instance();
        }

        public static T? GetInstance()
        {
            return _Instance;
        }
    }
}
