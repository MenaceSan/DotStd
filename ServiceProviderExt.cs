using System;
using System.Collections.Generic;
using System.Text;

namespace DotStd.ServiceExt
{
    public static class ServiceProviderExt
    {
        /// <summary>
        /// Get a service by its interface
        /// e.g. ServiceProvider.Instance().GetService<ILogger>()
        /// NOTE: This conflicts with Microsoft.Extensions.DependencyInjection 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="provider"></param>
        /// <returns></returns>
        public static T? GetService<T>(this IServiceProvider provider)
        {
            if (provider == null)
                return default(T);
            object? serviceO = provider.GetService(typeof(T));
            if (serviceO == null)
                return default(T);
            return (T)serviceO;
        }
    }
}
