using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Refit;
using Scheduler.Application.Interfaces;
using Scheduler.Domain.Repositories;
using Scheduler.Infrastructure.Caching;
using Scheduler.Infrastructure.DataAccess;
using Scheduler.Infrastructure.ExternalServices;
using Scheduler.Infrastructure.InternalClients;
using Scheduler.Infrastructure.InternalServices;

namespace Scheduler.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static void AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<SchedulerDbContext>(options =>
        {
            // Read lazily (not captured into a local before this call) — this delegate runs
            // at DbContext-resolution time, after WebApplicationFactory's test-only
            // ConfigureAppConfiguration override has been merged into IConfiguration. Reading
            // it eagerly here previously captured appsettings.json's value before the test
            // override applied, silently pointing integration tests at an unmigrated db file.
            options.UseSqlite(configuration.GetConnectionString("SchedulerDb"));
            // SQL Server is the production target — swap via connection string + provider:
            // options.UseSqlServer(configuration.GetConnectionString("SchedulerDb"));
        });

        services.AddMemoryCache();

        services.AddScoped<IAppointmentRepository, AppointmentRepository>();
        services.AddScoped<IAvailabilityCache, MemoryAvailabilityCache>();

        // Placeholders for this assessment — each internal service (Dealership/Technician/
        // Service Bay) swaps from its Mock*Provider to a real Refit-backed provider the moment
        // InfrastructureClients:<Service>:Http:BaseUrl is configured — see AddHttpServices
        // below and architecture.md Domain Assumptions for the swap-later plan.
        services.AddHttpServices(configuration);
        services.AddSingleton<INotificationService, MockNotificationService>();

        services.AddSingleton<IServiceTypeProvider>(_ =>
            new JsonServiceTypeProvider(Path.Combine(AppContext.BaseDirectory, "Data", "servicetypes.json")));
    }

    private static void AddHttpServices(this IServiceCollection services, IConfiguration configuration)
    {
        var dealershipService = configuration.GetSection("InfrastructureClients:DealershipService:Http:BaseUrl").Value;
        if (!string.IsNullOrEmpty(dealershipService))
        {
            services.AddTransient<IDealershipProvider, DealershipProvider>();
            services.AddRefitClient<IDealershipHttpClient>()
                .ConfigureHttpClient(c =>
                    c.BaseAddress = new Uri(dealershipService));
        }
        else
        {
            services.AddSingleton<IDealershipProvider, MockDealershipProvider>();
        }

        var technicianService = configuration.GetSection("InfrastructureClients:TechnicianService:Http:BaseUrl").Value;
        if (!string.IsNullOrEmpty(technicianService))
        {
            services.AddTransient<ITechnicianProvider, TechnicianProvider>();
            services.AddRefitClient<ITechnicianHttpClient>()
                .ConfigureHttpClient(c =>
                    c.BaseAddress = new Uri(technicianService));
        }
        else
        {
            services.AddSingleton<ITechnicianProvider, MockTechnicianProvider>();
        }

        var serviceBayService = configuration.GetSection("InfrastructureClients:ServiceBayService:Http:BaseUrl").Value;
        if (!string.IsNullOrEmpty(serviceBayService))
        {
            services.AddTransient<IServiceBayProvider, ServiceBayProvider>();
            services.AddRefitClient<IServiceBayHttpClient>()
                .ConfigureHttpClient(c =>
                    c.BaseAddress = new Uri(serviceBayService));
        }
        else
        {
            services.AddSingleton<IServiceBayProvider, MockServiceBayProvider>();
        }
    }
}
