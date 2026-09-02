using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Scalar.AspNetCore;
using Scheduler.Api.Extensions;
using Scheduler.Api.Filters;
using Scheduler.Api.Middleware;
using Scheduler.Api.OpenApi;
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

    // ApiResponseWrapperFilter wraps every controller result (success or failure) in the
    // standard ApiResponse envelope (Data/StatusCode/Message/Errors) — see its own comment
    // for why a global filter, not per-controller code, is where that logic lives.
    builder.Services.AddControllers(options => options.Filters.Add<ApiResponseWrapperFilter>());
    // CorrelationIdHeaderOperationTransformer documents the X-Correlation-Id header every
    // endpoint honors (see CorrelationIdMiddlewareExtensions); AppointmentsExampleOperationTransformer
    // attaches hand-written sample payloads to the Appointments endpoints — see both types'
    // own comments for why neither is derivable from attributes alone.
    builder.Services.AddOpenApi(options =>
    {
        options.AddOperationTransformer<CorrelationIdHeaderOperationTransformer>();
        options.AddOperationTransformer<AppointmentsExampleOperationTransformer>();
    });
    builder.Services.AddApplicationServices();
    builder.Services.AddInfrastructureServices(builder.Configuration);
    // ApiExceptionHandler builds the same ApiResponse envelope for unhandled exceptions —
    // the one path ApiResponseWrapperFilter can't reach (see its own comment). AddProblemDetails
    // stays registered as the framework-level fallback if a future exception handler doesn't
    // fully handle a request; ApiExceptionHandler always returns true, so it takes over first.
    builder.Services.AddExceptionHandler<ApiExceptionHandler>();
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
    // controller) become a 500 in the standard ApiResponse envelope — see ApiExceptionHandler,
    // registered above via AddExceptionHandler<ApiExceptionHandler>().
    app.UseExceptionHandler();

    app.UseHttpsRedirection();

    app.MapHealthChecks("/health");
    app.MapControllers();

    // Applies pending EF Core migrations (Appointment/AppointmentSlot schema — Dealership is
    // no longer a table this app seeds, see MockDealershipProvider) at startup — SQLite for
    // this assessment, SQL Server for production; same migrations apply to both.
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
