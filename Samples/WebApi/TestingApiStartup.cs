using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using SneakyLog.Extensions;

public class TestingApiStartup
{
    public IConfiguration _configuration { get; }

    public TestingApiStartup(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers();

        services.AddLogging(loggingBuilder =>
        {
            loggingBuilder.ClearProviders();
            loggingBuilder.AddConfiguration(_configuration);

            loggingBuilder.AddConsole(options =>
            {
                options.FormatterName = "CustomConsole";
            });
            
            loggingBuilder.AddConsoleFormatter<CustomConsoleFormatter, ConsoleFormatterOptions>();

        });

        // services.AddSneakyLog();

        AddLogicalServices(services);
    }

    public void AddLogicalServices(IServiceCollection services)
    {
        // Proxied
        // services.AddProxiedScoped(typeof(IPersonService), typeof(PersonService));

        // services.AddProxiedScoped(typeof(IPersonRepository), typeof(PersonRepository));
        // services.AddProxiedScoped(typeof(IHatsRepository), typeof(HatsRepository));
        // services.AddProxiedScoped(typeof(ICarsRepository), typeof(CarsRepository));

        // Normal
        services.AddScoped(typeof(IPersonService), typeof(PersonService));

        services.AddScoped(typeof(IPersonRepository), typeof(PersonRepository));
        services.AddScoped(typeof(IHatsRepository), typeof(HatsRepository));
        services.AddScoped(typeof(ICarsRepository), typeof(CarsRepository));
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILogger<TestingApiStartup> logger)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseRouting();
        // app.UseSneakyTracing();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
        });
    }
}