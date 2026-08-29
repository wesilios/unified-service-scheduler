using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Scheduler.Infrastructure.DataAccess;

namespace Scheduler.IntegrationTests;

// One isolated SQLite file per test class instance (xUnit creates a new instance per test
// method by default via the test class's own constructor, so callers that new this up in
// their constructor get full isolation between tests — no shared state, no cross-test
// double-booking interference).
public sealed class SchedulerApiFactory : WebApplicationFactory<Program>
{
    public readonly string DbPath = Path.Combine(Path.GetTempPath(), $"scheduler-test-{Guid.NewGuid():N}.db");

    public SchedulerApiFactory()
    {
        // Migrating via a standalone DbContext — independent of the DI-resolved host —
        // avoids entangling schema creation with WebApplicationFactory's own host-startup
        // machinery (DeferredHostBuilder/TestServer), which is not guaranteed to build the
        // host exactly once before the app's request pipeline starts serving. Applying the
        // migration here, before CreateClient() is ever called, is deterministic: it runs
        // exactly once, before any DI container touches this file.
        var options = new DbContextOptionsBuilder<SchedulerDbContext>()
            .UseSqlite($"Data Source={DbPath}")
            .Options;

        using var context = new SchedulerDbContext(options);
        context.Database.Migrate();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:SchedulerDb"] = $"Data Source={DbPath}"
            });
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            File.Delete(DbPath);
            File.Delete(DbPath + "-shm");
            File.Delete(DbPath + "-wal");
        }
    }
}
