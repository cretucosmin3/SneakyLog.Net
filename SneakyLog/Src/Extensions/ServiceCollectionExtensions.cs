
using Castle.DynamicProxy;
using Microsoft.Extensions.DependencyInjection;
using SneakyLog.Utilities;

namespace SneakyLog.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddSneakyLog(this IServiceCollection services, SneakyLogConfig? config = null)
    {
        if (config != null)
            SneakyLogContext.SetConfig(config);

        services.AddSingleton(new ProxyGenerator());
        services.AddScoped<SneakyInterceptor>();
        services.AddScoped<Microsoft.AspNetCore.HttpLogging.HttpLoggingInterceptorContext>();
    }

    public static void AddProxiedScoped(this IServiceCollection services, Type tInterface, Type tImplementation)
    {
        services.AddScoped(tImplementation);
        services.AddScoped(tInterface, serviceProvider =>
        {
            var proxyGenerator = serviceProvider.GetRequiredService<ProxyGenerator>();
            var actual = serviceProvider.GetRequiredService(tImplementation);
            var interceptor = serviceProvider.GetRequiredService<SneakyInterceptor>();

            return proxyGenerator.CreateInterfaceProxyWithTarget(tInterface, actual, interceptor);
        });
    }
}
