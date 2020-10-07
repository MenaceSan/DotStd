using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace DotStd
{
    // NOTE .NET Core and Std need NuGet for Configuration Support.

    public class ConfigApp
    {
        // App singleton config. AppDomain
        // Singleton for config info that applies to the app. App config only applies once.

        public static readonly Lazy<ConfigApp> _Instance = new Lazy<ConfigApp>();  // singleton.

        public int AppId { get; set; }         // int Id for logging. enum these in app space. This app is part of a Cluster PK .
        public int AppTypeId { get; private set; } // AppId enum these in app space. Never changed.

        public int MainThreadId { get; set; }        // Environment.CurrentManagedThreadId at start. AKA 'GUIThread'.
        public bool IsOnMainThread => Environment.CurrentManagedThreadId == MainThreadId;    // caller is on main thread? Equiv to IsInvokeRequired()

        private string _AppName;
        public string AppName
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

        public int AppVersion { get; private set; }  // (Major.Minor.Build) encoded as an int for sorting migration data.
        public string AppRevision { get; private set; }   // extra Version control tag.
        public string AppVersion3Str
        {
            // AppVersion should be read from Version.targets.template.
            // Might be in the format "1.2.3.4-EXTRA"
            get { return VersionUtil.ToVersionStr(AppVersion); }
        }
        public string AppVersionStr
        {
            // AppVersion should be read from Version.targets.template.
            // Might be in the format "1.2.3.4-EXTRA"
            get { return VersionUtil.ToVersionStr(AppVersion, AppRevision); }
        }

        private bool _IsUnitTesting_Checked = false;    // have i checked s_IsUnitTesting ?
        private bool _IsUnitTesting;                // cached state of IsUnitTestingX after s_IsUnitTesting_Checked

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

        public bool IsUnitTesting()
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

        private string _BaseDir;        // The base directory (for the app) we will use to find resource files.

        public string BaseDirectory
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

        public void SetUnitTesting(string baseDir)
        {
            // Declare that I am in unit test mode. set my _BaseDir
            // Assume IsUnitTesting

            _BaseDir = baseDir;
        }

        // The applications top level config read from some config file. May be redirected for testing.
        // get app global config info. support IServiceProvider.
        public ConfigInfoBase ConfigInfo { get; private set; }

        public void SetConfigInfo(ConfigInfoBase cfgInfo, int appId, string appName)
        {
            // Set app global config info.
            // MUST do this when first stating an app.
            // cfgInfo = new ConfigInfoServer()

            ConfigInfo = cfgInfo;
            AppId = appId;  // What app in the cluster am i ? may be updated later?
            AppTypeId = appId;
            _AppName = appName;
            MainThreadId = Environment.CurrentManagedThreadId;
        }

        public void SetConfigInfo(ConfigInfoBase cfgInfo, Enum appId)
        {
            // Set app global config info.
            SetConfigInfo(cfgInfo, appId.ToInt(), appId.ToDescription());
        }

        public void SetAppVersion(Assembly asm)
        {
            // Set version based on System.Version metadata in the top assembly.
            // asm = System.Reflection.Assembly.GetExecutingAssembly()
            // TODO Store and Get extra string tags for GIT hash id, etc ? [assembly: AssemblyInformationalVersion("13.3.1.74-g5224f3b")]
            // https://stackoverflow.com/questions/15141338/embed-git-commit-hash-in-a-net-dll

            System.Version ver = asm.GetName().Version;
            AppVersion = VersionUtil.ToVersionInt(ver);
            AppRevision = ver.Revision.ToString();
        }

        public static bool IsDebugging()
        {
            // Are we actively being debugged?
            return System.Diagnostics.Debugger.IsAttached;
        }

        public static bool IsInDocker => (Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == SerializeUtil.kTrue); 

        public static string EnvironmentName => Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");   // related to ConfigInfo.EnvironMode


        static int _Pid = ValidState.kInvalidId;
        public static int GetProcessId()
        {
            // The apps ProcessId can not change during its life.
            if (_Pid == ValidState.kInvalidId)
            {
                _Pid = Process.GetCurrentProcess().Id;
            }
            return _Pid;
        }

    }
}
