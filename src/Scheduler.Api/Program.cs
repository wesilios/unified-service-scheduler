using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Scalar.AspNetCore;
using Scheduler.Api.Extensions;
using Scheduler.Api.Middleware;
using Scheduler.Application;
using Scheduler.Infrastructure;
using Scheduler.Infrastructure.DataAccess;
using Serilog;

const string serviceName = "Scheduler.Api";

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting {ServiceName}", serviceName);

    var builder = WebApplication.CreateBuilder(args);

    // Serilog config (sinks, output template, level overrides) lives in serilog.json —
    // see that file for details, not here.
    builder.Configuration.AddJsonFile("serilog.json", optional: false, reloadOnChange: true);
    builder.Host.UseSerilog((context, _, configuration) => configuration.ReadFrom.Configuration(context.Configuration));

    builder.Services.AddControllers();
    builder.Services.AddOpenApi();
    builder.Services.AddApplicationServices();
    builder.Services.AddInfrastructureServices(builder.Configuration);
    builder.Services.AddProblemDetails();
    builder.Services.AddHealthChecks();
    builder.Services.AddSchedulerObservability(serviceName);

    var app = builder.Build();

    app.UseCorrelationId();
    app.UseSerilogRequestLogging();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.MapScalarApiReference();
    }

    // Unhandled exceptions (anything not already mapped to a 400/409 result in the
    // controller) become an RFC 9457 ProblemDetails response instead of a raw 500.
    app.UseExceptionHandler();

    app.UseHttpsRedirection();

    app.MapHealthChecks("/health");
    app.MapControllers();

    // Applies pending EF Core migrations (schema + seeded Dealership) at startup — SQLite
    // for this assessment, SQL Server for production; same migrations apply to both.
    // Skipped under "Testing": Scheduler.IntegrationTests' SchedulerApiFactory applies the
    // migration itself, via a standalone DbContext, before the host is ever built — see
    // SchedulerApiFactory's constructor for why (WebApplicationFactory's own host-startup
    // machinery isn't a safe place to run a non-idempotent schema migration from).
    if (!app.Environment.IsEnvironment("Testing"))
    {
        using var scope = app.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<SchedulerDbContext>().Database.Migrate();
    }

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    // HostAbortedException is EF Core design-time tooling deliberately stopping the host
    // after Build() to inspect the DbContext (e.g. `dotnet ef migrations add`) — expected,
    // not a real failure. Anything else here is a genuine unhandled startup failure.
    Log.Fatal(ex, "{ServiceName} terminated unexpectedly", serviceName);
}
finally
{
    Log.CloseAndFlush();
}

// Exposes Program to Scheduler.IntegrationTests via WebApplicationFactory<Program> — the
// top-level-statements Program class is otherwise implicitly internal.
public partial class Program;
