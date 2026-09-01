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
        // InfrastructureClients:<Service>:Http:BaseUrl is configured — see
        // AddInternalServiceProviders below and architecture.md Domain Assumptions for the
        // swap-later plan.
        services.AddInternalServiceProviders(configuration);
        services.AddSingleton<INotificationService, MockNotificationService>();

        services.AddSingleton<IServiceTypeProvider>(_ =>
            new JsonServiceTypeProvider(Path.Combine(AppContext.BaseDirectory, "Data", "servicetypes.json")));
    }

    // Named for the contract (registers the Provider abstractions, real-or-mock, config-driven)
    // rather than "AddHttpServices" — half of each branch below registers an in-memory mock with
    // no HTTP involved at all, so a name promising HTTP unconditionally would be misleading. See
    // .agent/skills/clean-code/SKILL.md §2.
    private static void AddInternalServiceProviders(this IServiceCollection services, IConfiguration configuration)
    {
        var dealershipServiceBaseUrl = configuration.GetSection("InfrastructureClients:DealershipService:Http:BaseUrl").Value;
        if (!string.IsNullOrEmpty(dealershipServiceBaseUrl))
        {
            services.AddTransient<IDealershipProvider, DealershipProvider>();
            services.AddRefitClient<IDealershipHttpClient>()
                .ConfigureHttpClient(c =>
                    c.BaseAddress = new Uri(dealershipServiceBaseUrl));
        }
        else
        {
            services.AddSingleton<IDealershipProvider, MockDealershipProvider>();
        }

        var technicianServiceBaseUrl = configuration.GetSection("InfrastructureClients:TechnicianService:Http:BaseUrl").Value;
        if (!string.IsNullOrEmpty(technicianServiceBaseUrl))
        {
            services.AddTransient<ITechnicianProvider, TechnicianProvider>();
            services.AddRefitClient<ITechnicianHttpClient>()
                .ConfigureHttpClient(c =>
                    c.BaseAddress = new Uri(technicianServiceBaseUrl));
        }
        else
        {
            services.AddSingleton<ITechnicianProvider, MockTechnicianProvider>();
        }

        var serviceBayServiceBaseUrl = configuration.GetSection("InfrastructureClients:ServiceBayService:Http:BaseUrl").Value;
        if (!string.IsNullOrEmpty(serviceBayServiceBaseUrl))
        {
            services.AddTransient<IServiceBayProvider, ServiceBayProvider>();
            services.AddRefitClient<IServiceBayHttpClient>()
                .ConfigureHttpClient(c =>
                    c.BaseAddress = new Uri(serviceBayServiceBaseUrl));
        }
        else
        {
            services.AddSingleton<IServiceBayProvider, MockServiceBayProvider>();
        }
    }
}
