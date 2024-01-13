using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace DotStd
{
    /// <summary>
    /// global (in app) service provider lookup. manage application singletons.
    /// We should use the default global DI provider instead of this! like app.ApplicationServices
    /// NOTE: This conflicts with Microsoft.Extensions.DependencyInjection.ServiceProvider
    /// TODO Get rid of all other uses of _Instance. singleton. Replace this with ASP or other versions of a DI/IServiceProvider.
    /// </summary>
    public class ServiceProvider : Singleton<IServiceProvider>, IServiceProvider
    {
        private readonly Dictionary<int, object> _Services = new(); // Like IServiceCollection.

        public void AddSingleton(Type t, object service)
        {
            // Configure a service by its interface . used for DI registration.
            int hashCode = t.GetHashCode();
            _Services[hashCode] = service;
        }

        public void AddSingleton<T>(T service) where T : class
        {
            // Configure a service by its interface T. used for DI registration.
            AddSingleton(typeof(T), service);
        }

        public void AddSingleton<TService, TImplementation>(TService service)
            where TService : class
            where TImplementation : class, TService
        {
            // Add as both the implementation and the interface.
            AddSingleton(typeof(TService), service);
            AddSingleton(typeof(TImplementation), service);
        }

        public object? GetService(Type serviceType)  // IServiceProvider
        {
            // Get a service. use the template/generic version of this. ServiceProviderExt.GetService<T>()
            // implement IServiceProvider. DI.

            if (_Services.TryGetValue(serviceType.GetHashCode(), out object? serviceO))
            {
                return serviceO;
            }
            if (serviceType == typeof(ILogger))
            {
                // ILogger is special.
                // Create a default singleton logger on demand. never return null.
                ILogger serviceL = new LoggerBase();    // just log to the debugger by default.
                AddSingleton<ILogger, LoggerBase>(serviceL);
                return serviceL;
            }
            return null;
        }
    }
}
