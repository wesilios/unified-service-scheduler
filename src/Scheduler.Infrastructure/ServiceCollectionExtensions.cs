using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Scheduler.Infrastructure.DataAccess;

namespace Scheduler.Infrastructure;

public static class ServiceCollectionExtensions
{
    private static void AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("SchedulerDb");

        services.AddDbContextFactory<SchedulerDbContext>(options =>
        {
            options.UseSqlite(connectionString);
            // options.UseSqlServer(connectionString);
        });
    }
}