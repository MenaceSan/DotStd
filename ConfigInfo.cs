using System;
using System.Collections.Generic;

namespace DotStd
{
    public enum ConfigMode
    {
        //! What type of deployment config is this ? Can have sub categories. Postfix by number for sub type.
        //! Assume upper case. 
        //! Similar to .NET Core IHostingEnvironment.IsDevelopment or IHostingEnvironment.EnvironmentName

        DEV,        // local deploy of code, maybe shared db or local db. May have variations, Dev3, Dev2 etc. AKA "Development" in Core
        TEST,       // Testing server for QA people usually.
        STAGING,    // Optional staging server, More public access. Some projects skip this mode.
        PROD,       // prod server seen by customers. AKA "Production" in Core
    }

    public abstract class ConfigInfoBase : IPropertyGetter, IServiceProvider
    {
        // Common config files stuff. Regardless of where the config info comes from.
        // Might be .NET core or framework.
        // Similar to IConfiguration

        public const string kApps = "Apps:";    // AKA appSettings
        public const string kConnectionStrings = "ConnectionStrings:";      // ConnectionStrings

        public const string kAppsConfigMode = "Apps:ConfigMode";       // What mode is this app running? Tag in appSettings. AKA EnvironmentName
        public const string kAppsLogFileDir = "Apps:LogFileDir";       // Where to put my local log files.

        // ConnectionString, MainUrl, AdminUrl, etc.

        // ConfigMode = Is this app running in Dev, Test or Prod mode ?  kAppsConfigMode 
        // Equiv to IHostingEnvironment.EnvironmentName
        public string ConfigMode { get; protected set; }    // What kAppsConfigMode does this app run in ? "Prod","Test","Dev", "Dev2", "Dev3"

        private Dictionary<int, object> Services = new Dictionary<int, object>();

        public object GetService(Type serviceType)  // IServiceProvider
        {
            // implement IServiceProvider
            object serviceO;
            if (Services.TryGetValue(serviceType.GetHashCode(), out serviceO))
            {
                return serviceO;
            }
            if (serviceType == typeof(ILogger))
            {
                // Create a default logger. never return null.
                ILogger serviceL = new LoggerBase();    // just log to the debugger by default.
                SetService(serviceL);
                return serviceL;
            }
            return null;
        }

        public T GetService<T>()
        {
            // My service locater. Like IServiceCollection.
            // e.g. ConfigApp.ConfigInfo.GetService<ILogger>()

            object serviceO;
            if (Services.TryGetValue(typeof(T).GetHashCode(), out serviceO))
            {
                return (T)serviceO;
            }
            if (typeof(T) == typeof(ILogger))
            {
                // Create a default logger. never return null.
                ILogger serviceL = new LoggerBase();    // just log to the debugger by default.
                SetService(serviceL);
                return (T)serviceL;
            }
            return default(T);
        }

        public void SetService<T>(T service) where T : class
        {
            // Configure a service by its interface .
            Services.Add(typeof(T).GetHashCode(), service);
        }

        public ILogger Logger { get { return GetService<ILogger>(); } }

        public bool isConfigModeLike(ConfigMode eConfigMode)
        {
            // Prefix match ConfigMode.
            // match ConfigMode but allow extension. e.g. Dev1 is the same as Dev
            return ConfigMode.ToUpper().StartsWith(eConfigMode.ToString());
        }
        public bool isConfigMode(string s)
        {
            // Exact match ConfigMode
            return String.Compare(ConfigMode, s, StringComparison.OrdinalIgnoreCase) == 0;
        }
        public bool isConfigMode(ConfigMode eConfigMode)
        {
            // Exact match ConfigMode
            return isConfigMode(eConfigMode.ToString());
        }
        public bool isConfigModeProd()
        {
            return isConfigMode(DotStd.ConfigMode.PROD);
        }

        public static string GetConfigModeFolder(string sConfigMode)
        {
            // use a special folder for a given config mode.
            // Use ConfigMode as a folder Name only if not Prod mode.
            if (string.IsNullOrWhiteSpace(sConfigMode))
                return "";  // FAIL!!
            sConfigMode = sConfigMode.ToUpper();
            if (sConfigMode == DotStd.ConfigMode.PROD.ToString())
                return "";
            return sConfigMode;
        }

        public string ConfigModeFolder
        {
            // Used for Mode display.
            get { return GetConfigModeFolder(this.ConfigMode); }
        }

        // Maybe prefixed with "Apps" or "ConnectionStrings"
        public abstract object GetPropertyValue(string name);

        public string GetSetting(string name)
        {
            // GetPropertyValue cast to string.
            var o = GetPropertyValue(name);
            return o?.ToString();
        }
    }

    public class ConfigInfoCore : ConfigInfoBase
    {
        // Support for the new JSON .NET core config style.
        // Wrapper for .NET Core IConfiguration

        public static Microsoft.Extensions.Configuration.IConfiguration _Configuration;       // .NET Core extension for JSON config.

        public ConfigInfoCore(Microsoft.Extensions.Configuration.IConfiguration config)
        {
            // config may be IConfigurationRoot or IConfigurationSection
            _Configuration = config;
            var sec = config.GetSection(kAppsConfigMode);
            ConfigMode = sec?.Value;
            ValidateArgument.EnsureNotNullOrWhiteSpace(ConfigMode, nameof(ConfigMode));  // MUSt have ConfigMode
        }

        public override object GetPropertyValue(string name)
        {
            // sName = "Sec:child"
            if (_Configuration != null)
            {
                var sec = _Configuration.GetSection(name);
                return sec?.Value;
            }
            return null;
        }
    }
}
