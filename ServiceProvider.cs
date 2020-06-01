using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace DotStd
{
    public class ServiceProvider : IServiceProvider
    {
        // Fallback global service provider. singletons.
        // We should use the default global DI provider instead of this! like app.ApplicationServices
        // NOTE: This conflicts with Microsoft.Extensions.DependencyInjection.ServiceProvider

        public static IServiceProvider _Instance;        // TODO Get rid of all other uses of _Instance. singleton.

        private readonly Dictionary<int, object> Services = new Dictionary<int, object>(); // Like IServiceCollection.

        public static IServiceProvider Get()
        {
            if (_Instance != null)
            {
                return _Instance;
            }
            _Instance = new ServiceProvider();
            return (ServiceProvider)_Instance;
        }

        public static ServiceProvider InitServiceProvider()
        {
            // Use this to add new singletons.
            if (_Instance != null)
            {
                if (_Instance is ServiceProvider)
                    return (ServiceProvider) _Instance;

                // This should never happen!
                return null;
            }
            _Instance = new ServiceProvider();
            return (ServiceProvider)_Instance;
        }

        public void AddSingleton(Type t, object service)
        {
            // Configure a service by its interface . used for DI registration.
            int hashCode = t.GetHashCode();
            Services[hashCode] = service;
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
            AddSingleton(typeof(TService), service);
            AddSingleton(typeof(TImplementation), service);
        }

        public object GetService(Type serviceType)  // IServiceProvider
        {
            // Get a service.
            // implement IServiceProvider. DI.

            if (Services.TryGetValue(serviceType.GetHashCode(), out object serviceO))
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
