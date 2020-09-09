using System;
using System.Collections.Generic;
using System.Text;

namespace DotStd.ServiceExt
{

    public static class ServiceProviderExt
    {
        // NOTE: This conflicts with Microsoft.Extensions.DependencyInjection 
        public static T GetService<T>(this IServiceProvider provider)
        {
            // Get a service.
            // e.g. ServiceProvider._Instance.GetService<ILogger>()
            if (provider == null)
                return default;
            object serviceO = provider.GetService(typeof(T));
            if (serviceO == null)
                return default;
            return (T)serviceO;
        }
    }
}
