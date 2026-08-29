using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Scheduler.Application.Interfaces;
using Scheduler.Infrastructure.Caching;
using Scheduler.Infrastructure.DataAccess;
using Scheduler.Infrastructure.ExternalServices;

namespace Scheduler.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("SchedulerDb");

        services.AddDbContext<SchedulerDbContext>(options =>
        {
            options.UseSqlite(connectionString);
            // SQL Server is the production target — swap via connection string + provider:
            // options.UseSqlServer(connectionString);
        });

        services.AddMemoryCache();

        services.AddScoped<IAppointmentRepository, AppointmentRepository>();
        services.AddScoped<IDealershipRepository, DealershipRepository>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IAvailabilityCache, MemoryAvailabilityCache>();

        // Placeholders for this assessment — see architecture.md Domain Assumptions for
        // the ITechnicianHttpClient/IServiceBayHttpClient (Refit, unwired) swap-later plan.
        services.AddSingleton<ITechnicianService, MockTechnicianService>();
        services.AddSingleton<IServiceBayService, MockServiceBayService>();
        services.AddSingleton<INotificationService, MockNotificationService>();

        services.AddSingleton<IServiceTypeProvider>(_ =>
            new JsonServiceTypeProvider(Path.Combine(AppContext.BaseDirectory, "Data", "servicetypes.json")));

        return services;
    }
}
