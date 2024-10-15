using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Security.AccessControl;

namespace DotStd
{
    /// <summary>
    /// Singleton for info that applies to the running app/process instance.
    /// only applies once. AppDomain
    /// NOTE .NET Core and Std need NuGet for Configuration Support.
    /// </summary>
    public class AppRoot : Singleton<AppRoot>
    {
        public readonly int AppTypeId;      // AppType enum these in app space. What type of app is this ? Never changed. readonly

        /// <summary>
        /// (Major.Minor.Build) encoded as an int for sorting migration data.
        /// Should match SchemaVersion for Db if applicable.
        /// </summary>
        public readonly int AppVersion;
        public readonly string AppRevision;   // extra Version control tag.

        /// <summary>
        /// Get Name for this process. default to EXE name.
        /// </summary>
        public readonly string AppName;

        /// <summary>
        /// AppVersion should be read from Version.targets.template.
        /// Convert to format (Major.Minor.Build)
        /// </summary>
        public string AppVersion3Str => VersionUtil.ToVersionStr(AppVersion);

        /// <summary>
        /// AppVersion should be read from Version.targets.template.
        /// Might be in the format "1.2.3.4-EXTRA"
        /// </summary>
        public string AppVersionStr => VersionUtil.ToVersionStr(AppVersion, AppRevision);

        static int _Pid = ValidState.kInvalidId;    // cache local PID
        /// <summary>
        /// The apps local ProcessId can not change during its life.
        /// </summary>
        /// <returns></returns>
        public static int GetProcessId()
        {
            if (_Pid == ValidState.kInvalidId)
            {
                _Pid = Environment.ProcessId;
            }
            return _Pid;
        }

        public readonly int MainThreadId;        // local Environment.CurrentManagedThreadId at start. AKA 'GUIThread'.
        public bool IsOnMainThread => Environment.CurrentManagedThreadId == MainThreadId;    // caller is on main thread? Equiv to IsInvokeRequired()

        public AppRoot(int appTypeId, Assembly asmTop)
        {
            AppTypeId = appTypeId;
            MainThreadId = Environment.CurrentManagedThreadId;
            AppName = Path.GetFileNameWithoutExtension(System.AppDomain.CurrentDomain.FriendlyName);

            /// Set version based on System.Version metadata in the top assembly.
            /// TODO Store and Get extra string tags for GIT hash id, etc ? [assembly: AssemblyInformationalVersion("13.3.1.74-g5224f3b")]
            /// https://stackoverflow.com/questions/15141338/embed-git-commit-hash-in-a-net-dll
            System.Version? ver = asmTop.GetName().Version;
            if (ver == null)
            {
                AppVersion = 0;
                AppRevision = ValidState.kInvalidName;
            }
            else
            {
                AppVersion = VersionUtil.ToVersion32(ver); // Can be compared with SchemaVersion for any db.
                AppRevision = ver.Revision.ToString();
            }
        }

        public static bool IsUnitTestingX()
        {
            // Determine if we are running inside a unit test. detect any UnitTestFramework version.
            try
            {
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (asm.FullName == null)
                        continue;
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

        private bool _IsUnitTesting_Checked = false;    // have i checked _IsUnitTesting ?
        private bool _IsUnitTesting;                // cached state of IsUnitTestingX after _IsUnitTesting_Checked

        /// <summary>
        /// Determine if we are running inside a unit test. detect any version.
        /// </summary>
        /// <returns></returns>
        public bool IsUnitTesting
        {
            get
            {
                if (!_IsUnitTesting_Checked) // do this just once. Cached
                {
                    _IsUnitTesting = IsUnitTestingX();
                    _IsUnitTesting_Checked = true;
                }
                return _IsUnitTesting;
            }
        }

        private string? _BaseDir;        // The base directory (for the app) we will use to find resource files. e.g. "C:\FourTe\BinDev\AdminWeb"

        /// <summary>
        /// What is my base install directory? i have resource files here. Assume Read Only.
        /// i may be a web app or not. don't use HttpContext.Current.Server.MapPath
        /// Similar to IHostingEnvironment.ContentRootPath
        /// </summary>
        [MemberNotNull(nameof(_BaseDir))]
        public string BaseDirectory
        {
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

        /// <summary>
        /// Declare that I am in unit test mode. set my _BaseDir
        /// Assume IsUnitTesting
        /// </summary>
        /// <param name="baseDir"></param>
        [MemberNotNull(nameof(_BaseDir))]
        public void SetUnitTesting(string baseDir)
        {
            _BaseDir = baseDir;
        }

        /// <summary>
        /// The applications top level config read from some config file. May be redirected for testing.
        /// get app global config info. support IServiceProvider.
        /// </summary>
        public ConfigInfoBase? _ConfigInfo;
        [MemberNotNull(nameof(_ConfigInfo))]
        public ConfigInfoBase ConfigInfo => ValidState.GetNotNull(_ConfigInfo, nameof(_ConfigInfo));   // must call SetConfigInfo()

        /// <summary>
        /// Set app global config info.
        /// MUST do this when first stating an app.
        /// </summary>
        /// <param name="cfgInfo">new ConfigInfoServer()</param>
        /// <param name="appProcId">Cluster PK</param>
        /// <param name="appName"></param>
        [MemberNotNull(nameof(_ConfigInfo))]
        public void SetConfigInfo(ConfigInfoBase cfgInfo)
        {
            _ConfigInfo = cfgInfo;
        }

        public static bool IsDebugging()
        {
            // Are we actively being debugged?
            return System.Diagnostics.Debugger.IsAttached;
        }

        public static bool IsInDocker => (Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == SerializeUtil.kTrue);

        public static string? EnvironmentName => Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");   // related to ConfigInfo.EnvironMode
    }
}
