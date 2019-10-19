using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace DotStd
{
    public enum ConfigMode
    {
        //! What type of deployment config is this ? AKA Environment.
        //! Can have sub categories. Postfix by number for sub type.
        //! Assume upper case. 
        //! Similar to .NET Core IHostingEnvironment.IsDevelopment or IHostingEnvironment.EnvironmentName

        [Description("Development")]
        DEV,        // local deploy of code, maybe shared db or local db. May have variations, Dev3, Dev2 etc. AKA "Development" in Core
        TEST,       // Testing server for QA people usually.
        STAGING,    // Optional staging server, More public access. Some projects skip this mode.
        [Description("Production")]
        PROD,       // prod server seen by customers. AKA "Production" in Core
    }

    public class ConfigInfoCore : IPropertyGetter
    {
        // Support for the new .NET core JSON config file style.
        // Wrapper for .NET Core IConfiguration

        public static Microsoft.Extensions.Configuration.IConfiguration _Configuration;       // .NET Core extension for JSON config.

        public ConfigInfoCore(Microsoft.Extensions.Configuration.IConfiguration config)
        {
            // config may be IConfigurationRoot or IConfigurationSection
            _Configuration = config;
        }

        public virtual object GetPropertyValue(string name)
        {
            // sName = "Sec:child" IPropertyGetter
            if (_Configuration != null)
            {
                var sec = _Configuration.GetSection(name);
                return sec?.Value;
            }
            return null;
        }
    }

    public class ConfigInfoBase : IPropertyGetter, IServiceProvider
    {
        // Common config files stuff. Regardless of where the config info comes from.
        // abstracted _ConfigSource Might be .NET core JSON or framework XML.
        // Similar to IConfiguration

        public const string kApps = "Apps:";    // AKA appSettings
        public const string kConnectionStrings = "ConnectionStrings:";      // ConnectionStrings to db.
        public const string kSmtp = "Smtp:";    // configure how to send emails. Old XML config had weird format.

        public const string kAppsConfigMode = "Apps:ConfigMode";       // What mode is this app running? Tag in appSettings. AKA EnvironmentName
        public const string kAppsLogFileDir = "Apps:LogFileDir";       // Where to put my local log files.

        // ConfigMode = Is this app running in Dev, Test or Prod mode ?  kAppsConfigMode 
        // Equiv to IHostingEnvironment.EnvironmentName
        public string ConfigMode { get; protected set; }    // What kAppsConfigMode does this app run in ? "Prod","Test","Dev", "Dev2", "Dev3"

        private readonly IPropertyGetter _ConfigSource;   // Get my config info from here.
        private readonly Dictionary<int, object> Services = new Dictionary<int, object>();

        public string ConnectionStringDef { get; protected set; }      // Primary/default db connection string. If i need one.

        public object GetService(Type serviceType)  // IServiceProvider
        {
            // implement IServiceProvider. DI.
          
            if (Services.TryGetValue(serviceType.GetHashCode(), out object serviceO))
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
 
            if (Services.TryGetValue(typeof(T).GetHashCode(), out object serviceO))
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
            return default;
        }

        public void SetService<T>(T service) where T : class
        {
            // Configure a service by its interface . used for DI registration.
            // ILogger is special.
            int hashCode = typeof(T).GetHashCode();
            Services[hashCode] = service;
        }

        public ILogger Logger { get { return GetService<ILogger>(); } }

        public bool IsConfigModeLike(ConfigMode configMode)
        {
            // Prefix match ConfigMode.
            // match ConfigMode but allow extension. e.g. Dev1 is the same as Dev
            if (ConfigMode == null)
                return false;
            return ConfigMode.ToUpper().StartsWith(configMode.ToString());
        }
        public bool IsConfigMode(string configMode)
        {
            // Exact match ConfigMode
            return String.Compare(ConfigMode, configMode, StringComparison.OrdinalIgnoreCase) == 0;
        }
        public bool IsConfigMode(ConfigMode configMode)
        {
            // Exact match ConfigMode
            return IsConfigMode(configMode.ToString());
        }
        public bool IsConfigModeProd()
        {
            return IsConfigMode(DotStd.ConfigMode.PROD);
        }

        public virtual object GetPropertyValue(string name)
        {
            // IPropertyGetter
            // Maybe prefixed with "Apps" or "ConnectionStrings"
            return _ConfigSource.GetPropertyValue(name);
        }

        public string GetSetting(string name)
        {
            // GetPropertyValue cast to string.
            var o = GetPropertyValue(name);
            return o?.ToString();
        }

        public ConfigInfoBase(IPropertyGetter configSource, string connectionStringName = null)
        {
            // Assign my config source. (file?)
            _ConfigSource = configSource;
            ConfigMode = GetSetting(kAppsConfigMode);
            ValidState.ThrowIfWhiteSpace(ConfigMode, nameof(ConfigMode));  // MUST have ConfigMode

            if (connectionStringName != null)
            {
                ConnectionStringDef = GetSetting(connectionStringName);
            }
        }
    }
}
