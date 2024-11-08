using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SneakyLog.Extensions;

public class Startup
{
    private readonly bool UsingSneaky = true;

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers();

        if (UsingSneaky)
        {
            services.AddSneakyLog();
            AddLogicalServices(services);
        }
        else
            AddLogicalServicesNormal(services);
    }

    public void AddLogicalServices(IServiceCollection services)
    {
        services.AddProxiedScoped(typeof(IAService), typeof(AService));
        services.AddProxiedScoped(typeof(IA1Service), typeof(A1Service));
        services.AddProxiedScoped(typeof(IBService), typeof(BService));
        services.AddProxiedScoped(typeof(IB1Service), typeof(B1Service));
        services.AddProxiedScoped(typeof(IB2Service), typeof(B2Service));
        services.AddProxiedScoped(typeof(IB3Service), typeof(B3Service));
    }

    public void AddLogicalServicesNormal(IServiceCollection services)
    {
        services.AddScoped(typeof(IAService), typeof(AService));
        services.AddScoped(typeof(IA1Service), typeof(A1Service));
        services.AddScoped(typeof(IBService), typeof(BService));
        services.AddScoped(typeof(IB1Service), typeof(B1Service));
        services.AddScoped(typeof(IB2Service), typeof(B2Service));
        services.AddScoped(typeof(IB3Service), typeof(B3Service));
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILogger<Startup> logger)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseRouting();

        if (UsingSneaky)
            app.UseSneakyTracing();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
        });
    }
}