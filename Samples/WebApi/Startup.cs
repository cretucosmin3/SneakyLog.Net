using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SneakyLog.Extensions;

public class Startup
{
    private readonly bool UsingSneakyLog = true;

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers();

        if (UsingSneakyLog)
        {
            services.AddSneakyLog();
            RegisterProxiedServices(services);
        }
        else
        {
            RegisterNormalServices(services);
        }
    }

    private void RegisterProxiedServices(IServiceCollection services)
    {
        services.AddProxiedScoped(typeof(IPersonService), typeof(PersonService));
        services.AddProxiedScoped(typeof(IPersonRepository), typeof(PersonRepository));
        services.AddProxiedScoped(typeof(IHatsRepository), typeof(HatsRepository));
        services.AddProxiedScoped(typeof(ICarsRepository), typeof(CarsRepository));
    }

    private void RegisterNormalServices(IServiceCollection services)
    {
        services.AddScoped(typeof(IPersonService), typeof(PersonService));
        services.AddScoped(typeof(IPersonRepository), typeof(PersonRepository));
        services.AddScoped(typeof(IHatsRepository), typeof(HatsRepository));
        services.AddScoped(typeof(ICarsRepository), typeof(CarsRepository));
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILogger<Startup> logger)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseRouting();

        if (UsingSneakyLog)
            app.UseSneakyTracing();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
        });
    }
}