using System.Diagnostics;
using Serilog.Context;

namespace Scheduler.Api.Middleware;

public static class CorrelationIdMiddlewareExtensions
{
    // Honors an incoming X-Correlation-Id if present, else mints one. Pushed into the
    // Serilog LogContext alongside the OpenTelemetry TraceId so every log line for this
    // request carries both — see architecture.md §7 Observability. Must run before
    // UseSerilogRequestLogging so its Request starting/finished lines are in scope too.
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app)
    {
        return app.Use(async (context, next) =>
        {
            var correlationId = context.Request.Headers.TryGetValue("X-Correlation-Id", out var existing)
                                 && !string.IsNullOrWhiteSpace(existing)
                ? existing.ToString()
                : Guid.NewGuid().ToString();

            context.Response.Headers["X-Correlation-Id"] = correlationId;

            var traceId = Activity.Current?.TraceId.ToString();

            using (LogContext.PushProperty("CorrelationId", correlationId))
            using (LogContext.PushProperty("TraceId", traceId))
            {
                await next();
            }
        });
    }
}
