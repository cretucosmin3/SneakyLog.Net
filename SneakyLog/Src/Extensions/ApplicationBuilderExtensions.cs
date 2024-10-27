using Microsoft.AspNetCore.Builder;

namespace SneakyLog.Extensions;

public static class RequestIdMiddlewareExtensions
{
    public static IApplicationBuilder UseSneakyTracing(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<SneakyMiddleware>();
    }
}