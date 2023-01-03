using System;
using System.Diagnostics.CodeAnalysis;

namespace DotStd
{
    public class Singleton<T>
    {
        protected static T? _Instance;  // singleton.

        public static void InitInstanceTest(T i)
        {
            // For debug/test. allow singleton replacement.
            _Instance = i;
        }

        public static T InitInstance(T i)
        {
            ValidState.ThrowIf(_Instance != null, nameof(_Instance));
            _Instance = i;
            return i;
        }

        [MemberNotNullWhen(returnValue: true, member: nameof(_Instance))]
        public static bool IsInit()
        {
            return _Instance != null;
        }

        [MemberNotNull(nameof(_Instance))]
        public static T Instance()
        {
            // Must be created by InitInstance call.
            ValidState.ThrowIfNull(_Instance, nameof(_Instance));
            return _Instance;
        }

        public static T? GetInstance()
        {
            return _Instance;
        }
    }
}
