using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace DotStd
{
    /// <summary>
    /// What type of deployment Environment is this ? AKA EnvironmentName.
    /// Can have sub categories. Postfix by number for sub type.
    /// Assume upper case. 
    /// Similar to .NET Core IHostingEnvironment.IsDevelopment or IHostingEnvironment.EnvironmentName
    /// </summary>
    public enum EnvironMode
    {
        [Description("Development")]        // EnvironmentName
        DEV,        // local deploy of code, maybe shared db or local db. May have variations, Dev3, Dev2 etc. AKA "Development" in Core
        TEST,       // Testing server for QA people usually. AKA "QA"
        STAGING,    // Optional staging server, More public access. Some projects skip this mode. AKA "QA-SIT"
        [Description("Production")]         // EnvironmentName
        PROD,       // prod server seen by customers. AKA "Production" in Core. trunk code branch.
    }

    /// <summary>
    /// Support for the new .NET core JSON config file style.
    /// Wrapper for .NET Core IConfiguration
    /// </summary>
    public class ConfigInfoCore : IPropertyGetter
    {
        public readonly Microsoft.Extensions.Configuration.IConfiguration _Configuration;       // .NET Core extension for JSON config.

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

        /// <summary>
        /// Get property from _Configuration
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public virtual object? GetPropertyValue(string name)
        {
            // sName = "Sec:child" IPropertyGetter

            if (_Configuration != null)
            {
                var sec = _Configuration.GetSection(name);
                object? val = sec?.Value;
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

    /// <summary>
    /// Common config files stuff for any app. Regardless of where the config info comes from.
    /// abstracted _ConfigSource Might be .NET core JSON or framework XML.
    /// Similar to IConfiguration
    /// </summary>
    public class ConfigInfoBase : IPropertyGetter 
    {
        public const string kApps = "Apps:";    // AKA appSettings
        public const string kConnectionStrings = "ConnectionStrings:";      // ConnectionStrings to db.
        public const string kSmtp = "Smtp:";    // configure how to send emails. Old XML config had weird format.

        public const string kAppsEnvironMode = "Apps:EnvironMode";       // What mode is this app running? Tag in appSettings. AKA EnvironmentName
        public const string kAppsLogFileDir = "Apps:LogFileDir";       // Where to put my local log files.

        // EnvironMode = Is this app running in Dev, Test or Prod mode ? 
        // Equiv to IHostingEnvironment.EnvironmentName
        public readonly string EnvironMode = DotStd.EnvironMode.DEV.ToString();    // What kAppsEnvironMode does this app run in ? "Prod","Test","Dev", "Dev2", "Dev3"

        private readonly IPropertyGetter _ConfigSource;   // Get my config info from here. Some file? e.g. ConfigInfoCore

        public string? ConnectionStringDef { get; protected set; }      // Primary/default db connection string. If i need one.
 
        public bool IsEnvironModeLike(EnvironMode environMode)
        {
            // Prefix match EnvironMode.
            // match EnvironMode but allow extension. e.g. Dev1 is the same as Dev
            return EnvironMode.ToUpper().StartsWith(environMode.ToString());
        }
        public bool IsEnvironMode(string environMode)
        {
            // Exact match EnvironMode
            return string.Compare(EnvironMode, environMode, StringComparison.OrdinalIgnoreCase) == 0;
        }
        public bool IsEnvironMode(EnvironMode environMode)
        {
            // Exact match EnvironMode
            return IsEnvironMode(environMode.ToString());
        }
        public bool IsEnvironModeProd()
        {
            return IsEnvironMode(DotStd.EnvironMode.PROD);
        }

        public virtual object? GetPropertyValue(string name)
        {
            // IPropertyGetter
            // Maybe prefixed with "Apps" or "ConnectionStrings"
            return _ConfigSource.GetPropertyValue(name);
        }

        public string? GetSetting(string name)
        {
            // GetPropertyValue cast to string. null = not present.
            var o = GetPropertyValue(name);
            return o?.ToString();
        }

        public ConfigInfoBase(IPropertyGetter configSource, string? connectionStringName = null)
        {
            // Assign my config source. (file?)
            _ConfigSource = configSource;
            EnvironMode = GetSetting(kAppsEnvironMode) ?? string.Empty;
            ValidState.ThrowIfWhiteSpace(EnvironMode, nameof(EnvironMode));  // MUST have EnvironMode

            if (connectionStringName != null)
            {
                ConnectionStringDef = GetSetting(connectionStringName);
            }
        }
    }
}
