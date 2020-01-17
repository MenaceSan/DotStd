using System;
using System.IO;

namespace DotStd
{
    // NOTE .NET Core and Std need NuGet for Configuration Support.

    public static class ConfigApp
    {
        // App singleton config. AppDomain
        // Singleton for config info that applies to the app. App config only applies once.

        public static int AppId { get; set; }         // int Id for logging. enum these in app space. This app is part of a Cluster PK .
        public static int AppTypeId { get; private set; } // AppId enum these in app space. Never changed.

        public static int MainThreadId { get; set; }        // Environment.CurrentManagedThreadId at start. AKA 'GUIThread'.
        public static bool IsOnMainThread => Environment.CurrentManagedThreadId == MainThreadId;    // caller is on main thread? Equiv to IsInvokeRequired()

        private static string _AppName;
        public static string AppName
        {
            get
            {
                if (_AppName == null)
                {
                    // We really shouldn't need to do this. Should be set via SetConfigInfo.
                    _AppName = Path.GetFileNameWithoutExtension(System.AppDomain.CurrentDomain.FriendlyName);
                }
                return _AppName;
            }
        }

        private static int AppVersion;  // (Major.Minor.Build) encoded as an int for sorting migration data.
        public static string AppVersionStr
        {
            get { return VersionUtil.ToVersionStr(AppVersion); }
        }

        private static bool _IsUnitTesting_Checked = false;    // have i checked s_IsUnitTesting ?
        private static bool _IsUnitTesting;                // cached state of IsUnitTestingX after s_IsUnitTesting_Checked

        public static bool IsUnitTestingX()
        {
            // Determine if we are running inside a unit test. detect any UnitTestFramework version.
            try
            {
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (asm.FullName.StartsWith("Microsoft.VisualStudio.QualityTools.UnitTestFramework"))   // VS2015
                        return true;
                    if (asm.FullName.StartsWith("Microsoft.TestPlatform.PlatformAbstractions"))  // VS2017
                        return true;
                    // "Microsoft.VisualStudio.TestPlatform.MSTestAdapter.PlaformServices"
                    // "Microsoft.VisualStudio.TestPlatform.MSTestAdapter.PlaformServices.Interface"
                    // "Microsoft.VisualStudio.TestPlatform.MSTest.TestAdapter
                    // "Microsoft.VisualStudio.TestPlatform.TestFramework
                    // "Microsoft.VisualStudio.TestPlatform.TestFramework.Extensions
                }
            }
            catch
            {
            }
            return false;
        }

        public static bool IsUnitTesting()
        {
            // Determine if we are running inside a unit test. detect any version.
            // Cached
            if (!_IsUnitTesting_Checked)
            {
                _IsUnitTesting = IsUnitTestingX();
                _IsUnitTesting_Checked = true;
            }
            return _IsUnitTesting;
        }

        private static string _BaseDir;        // The base directory (for the app) we will use to find resource files.

        public static string BaseDirectory
        {
            // What is my install directory? i have resource files here.
            // i may be a web app or not. don't use HttpContext.Current.Server.MapPath
            // Similar to IHostingEnvironment.ContentRootPath

            get
            {
                if (string.IsNullOrEmpty(_BaseDir))
                {
                    _BaseDir = AppDomain.CurrentDomain.BaseDirectory;

                    // Chop off the "\bin\*" part of the path to get back to root.
                    int i = _BaseDir.IndexOf(@"\bin\Debug");    // e.g. "\bin\Debug\netcoreapp2.1\" 
                    if (i < 0)
                    {
                        i = _BaseDir.IndexOf(@"\bin\Release");
                    }
                    if (i > 0)
                    {
                        _BaseDir = _BaseDir.Substring(0, i); // chop this off.
                    }
                }
                return _BaseDir;
            }
        }

        public static void SetUnitTesting(string baseDir)
        {
            // Declare that I am in unit test mode. set my _BaseDir
            // Assume IsUnitTesting

            _BaseDir = baseDir;
        }

        // The applications top level config read from some config file. May be redirected for testing.
        // get app global config info. support IServiceProvider.
        public static ConfigInfoBase ConfigInfo { get; private set; }

        public static void SetConfigInfo(ConfigInfoBase cfgInfo, int appId, string appName, int appVersion)
        {
            // Set app global config info.
            // MUST do this when first stating an app.
            // cfgInfo = new ConfigInfoServer()

            ConfigInfo = cfgInfo;
            AppId = appId;  // What app in the cluster am i ? may be updated later?
            AppTypeId = appId;
            _AppName = appName;
            AppVersion = appVersion;
            MainThreadId = Environment.CurrentManagedThreadId;
        }

        public static void SetConfigInfo(ConfigInfoBase cfgInfo, Enum appId, int appVersion)
        {
            // Set app global config info.
            SetConfigInfo(cfgInfo, appId.ToInt(), appId.ToDescription(), appVersion);
        }

        public static bool IsDebugging()
        {
            // Are we actively being debugged?
            return System.Diagnostics.Debugger.IsAttached;
        }

        public static bool IsInDocker => (Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == SerializeUtil.kTrue); 

        public static string EnvironmentName => Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");   // related to ConfigInfo.EnvironMode
    }
}
