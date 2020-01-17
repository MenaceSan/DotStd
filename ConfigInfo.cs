using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace DotStd
{
    public enum EnvironMode
    {
        //! What type of deployment Environment is this ? AKA EnvironmentName.
        //! Can have sub categories. Postfix by number for sub type.
        //! Assume upper case. 
        //! Similar to .NET Core IHostingEnvironment.IsDevelopment or IHostingEnvironment.EnvironmentName

        [Description("Development")]        // EnvironmentName
        DEV,        // local deploy of code, maybe shared db or local db. May have variations, Dev3, Dev2 etc. AKA "Development" in Core
        TEST,       // Testing server for QA people usually.
        STAGING,    // Optional staging server, More public access. Some projects skip this mode.
        [Description("Production")]         // EnvironmentName
        PROD,       // prod server seen by customers. AKA "Production" in Core. trunk code branch.
    }

    public class ConfigInfoCore : IPropertyGetter
    {
        // Support for the new .NET core JSON config file style.
        // Wrapper for .NET Core IConfiguration

        public static Microsoft.Extensions.Configuration.IConfiguration _Configuration;       // .NET Core extension for JSON config.

        private readonly string _EnvironMode;   // ONLY set via ASPNETCORE_ENVIRONMENT => EnvironmentName

        public ConfigInfoCore(Microsoft.Extensions.Configuration.IConfiguration config, string environmentName)
        {
            // config may be IConfigurationRoot or IConfigurationSection
            // ASSUME ASPNETCORE_ENVIRONMENT has been set and dictates what my EnvironMode is.
            _Configuration = config;

            if (string.Equals(environmentName, "Development", StringComparison.InvariantCultureIgnoreCase))
                environmentName = EnvironMode.DEV.ToString();
            if (string.Equals(environmentName, "Production", StringComparison.InvariantCultureIgnoreCase))
                environmentName = EnvironMode.PROD.ToString();

            _EnvironMode = environmentName;
        }

        public virtual object GetPropertyValue(string name)
        {
            // sName = "Sec:child" IPropertyGetter

            if (_Configuration != null)
            {
                var sec = _Configuration.GetSection(name);
                object val = sec?.Value;
                if (val != null)
                    return val;
            }

            if (string.Equals(name, ConfigInfoBase.kAppsEnvironMode, StringComparison.InvariantCultureIgnoreCase))
            {
                return _EnvironMode;
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

        public const string kAppsEnvironMode = "Apps:EnvironMode";       // What mode is this app running? Tag in appSettings. AKA EnvironmentName
        public const string kAppsLogFileDir = "Apps:LogFileDir";       // Where to put my local log files.

        // EnvironMode = Is this app running in Dev, Test or Prod mode ? 
        // Equiv to IHostingEnvironment.EnvironmentName
        public readonly string EnvironMode;    // What kAppsEnvironMode does this app run in ? "Prod","Test","Dev", "Dev2", "Dev3"

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
                AddService(serviceL);
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
                AddService(serviceL);
                return (T)serviceL;
            }
            return default;
        }

        public void AddService<T>(T service) where T : class
        {
            // Configure a service by its interface . used for DI registration.
            // ILogger is special.
            int hashCode = typeof(T).GetHashCode();
            Services[hashCode] = service;
        }

        public ILogger Logger { get { return GetService<ILogger>(); } }

        public bool IsEnvironModeLike(EnvironMode configMode)
        {
            // Prefix match EnvironMode.
            // match EnvironMode but allow extension. e.g. Dev1 is the same as Dev
            if (EnvironMode == null)
                return false;
            return EnvironMode.ToUpper().StartsWith(configMode.ToString());
        }
        public bool IsEnvironMode(string configMode)
        {
            // Exact match EnvironMode
            return String.Compare(EnvironMode, configMode, StringComparison.OrdinalIgnoreCase) == 0;
        }
        public bool IsEnvironMode(EnvironMode configMode)
        {
            // Exact match EnvironMode
            return IsEnvironMode(configMode.ToString());
        }
        public bool IsEnvironModeProd()
        {
            return IsEnvironMode(DotStd.EnvironMode.PROD);
        }

        public virtual object GetPropertyValue(string name)
        {
            // IPropertyGetter
            // Maybe prefixed with "Apps" or "ConnectionStrings"
            return _ConfigSource.GetPropertyValue(name);
        }

        public string GetSetting(string name)
        {
            // GetPropertyValue cast to string. null = not present.
            var o = GetPropertyValue(name);
            return o?.ToString();
        }

        public ConfigInfoBase(IPropertyGetter configSource, string connectionStringName = null)
        {
            // Assign my config source. (file?)
            _ConfigSource = configSource;
            EnvironMode = GetSetting(kAppsEnvironMode);
            ValidState.ThrowIfWhiteSpace(EnvironMode, nameof(EnvironMode));  // MUST have EnvironMode

            if (connectionStringName != null)
            {
                ConnectionStringDef = GetSetting(connectionStringName);
            }
        }
    }
}
