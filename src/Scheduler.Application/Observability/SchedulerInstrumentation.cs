using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Scheduler.Application.Observability;

// Central ActivitySource/Meter for the booking flow — spans map onto the Data Flow
// sequence diagram stages, metrics onto the ones defined in architecture.md §7.
// Pure BCL (System.Diagnostics), no package needed here; the OpenTelemetry SDK in
// Scheduler.Api subscribes to these by name ("Scheduler.Application") at startup.
public static class SchedulerInstrumentation
{
    public const string Name = "Scheduler.Application";

    public static readonly ActivitySource ActivitySource = new(Name);
    public static readonly Meter Meter = new(Name);

    // Booking outcome rate (201/400/409) — the headline business metric.
    public static readonly Counter<long> BookingOutcomes =
        Meter.CreateCounter<long>("scheduler.booking.outcomes", description: "Count of booking attempts by outcome status");

    // 409-conflict rate specifically — proxy for AppointmentSlot contention (see Cache
    // Strategy §5 / Future Evolution §10: the trigger signal for introducing Redis).
    public static readonly Counter<long> BookingConflicts =
        Meter.CreateCounter<long>("scheduler.booking.conflicts", description: "Count of 409 booking conflicts");

    // External mock-service call latency — instrumented at the interface boundary so it
    // survives the swap to real ITechnicianHttpClient/IServiceBayHttpClient unchanged.
    public static readonly Histogram<double> ExternalValidationDuration =
        Meter.CreateHistogram<double>("scheduler.external_validation.duration", unit: "ms",
            description: "Technician/ServiceBay existence-check latency");

    // Availability-check (GetOverlappingAsync) latency — the metric that would justify
    // moving IMemoryCache to Redis or adding an index.
    public static readonly Histogram<double> AvailabilityCheckDuration =
        Meter.CreateHistogram<double>("scheduler.availability_check.duration", unit: "ms",
            description: "Overlap read-check latency");
}
