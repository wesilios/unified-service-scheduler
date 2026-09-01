using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Scheduler.Application.Interfaces;
using Scheduler.Domain.Repositories;
using Scheduler.Infrastructure.Caching;
using Scheduler.Infrastructure.DataAccess;
using Scheduler.Infrastructure.ExternalServices;

namespace Scheduler.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
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

        // Placeholders for this assessment — see architecture.md Domain Assumptions for
        // the IDealershipHttpClient/ITechnicianHttpClient/IServiceBayHttpClient (Refit,
        // unwired) swap-later plan.
        services.AddSingleton<IDealershipProvider, MockDealershipProvider>();
        services.AddSingleton<ITechnicianProvider, MockTechnicianProvider>();
        services.AddSingleton<IServiceBayProvider, MockServiceBayProvider>();
        services.AddSingleton<INotificationService, MockNotificationService>();

        services.AddSingleton<IServiceTypeProvider>(_ =>
            new JsonServiceTypeProvider(Path.Combine(AppContext.BaseDirectory, "Data", "servicetypes.json")));

        return services;
    }
}
