using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Scheduler.Application.Observability;

namespace Scheduler.Api.Extensions;

public static class ObservabilityServiceCollectionExtensions
{
    public static IServiceCollection AddSchedulerObservability(this IServiceCollection services, string serviceName)
    {
        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(serviceName))
            .WithTracing(tracing => tracing
                // Spans mapped onto the Data Flow stages — see SchedulerInstrumentation.
                .AddSource(SchedulerInstrumentation.Name)
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddConsoleExporter())
            .WithMetrics(metrics => metrics
                .AddMeter(SchedulerInstrumentation.Name)
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddConsoleExporter());

        return services;
    }
}
