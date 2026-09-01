using System.Diagnostics;
using Scheduler.Application.Interfaces;
using Scheduler.Application.Observability;
using Scheduler.Application.Queries;
using Scheduler.Domain.Entities;
using Scheduler.Domain.Repositories;
using Scheduler.Domain.Services;
using Scheduler.Domain.ValueObjects;

namespace Scheduler.Application.Services;

// Shared by CreateAppointmentCommandHandler and CheckAvailabilityQueryHandler — both need
// the identical "is this Technician/ServiceBay/time valid and free" check (Data Flow up to
// and including the overlap read-check). AvailabilityStatus.Available means "proceed";
// CreateAppointmentCommandHandler additionally persists on success, CheckAvailabilityQuery
// just reports it.
public sealed class AppointmentAvailabilityChecker : IAppointmentAvailabilityChecker
{
    private readonly IDealershipProvider _dealershipProvider;
    private readonly ITechnicianProvider _technicianProvider;
    private readonly IServiceBayProvider _serviceBayProvider;
    private readonly IServiceTypeProvider _serviceTypeProvider;
    private readonly IAppointmentRepository _appointments;

    public AppointmentAvailabilityChecker(
        IDealershipProvider dealershipProvider,
        ITechnicianProvider technicianProvider,
        IServiceBayProvider serviceBayProvider,
        IServiceTypeProvider serviceTypeProvider,
        IAppointmentRepository appointments)
    {
        _dealershipProvider = dealershipProvider;
        _technicianProvider = technicianProvider;
        _serviceBayProvider = serviceBayProvider;
        _serviceTypeProvider = serviceTypeProvider;
        _appointments = appointments;
    }

    public async Task<AvailabilityCheckOutcome> CheckAsync(
        Guid dealershipId,
        Guid technicianId,
        Guid serviceBayId,
        string serviceTypeCode,
        DateTime startTime,
        CancellationToken cancellationToken = default)
    {
        ServiceType? serviceType;
        using (SchedulerInstrumentation.ActivitySource.StartActivity("ServiceType lookup"))
        {
            serviceType = await _serviceTypeProvider.TryGetAsync(serviceTypeCode, cancellationToken);
        }

        if (serviceType is null)
        {
            return AvailabilityCheckOutcome.Failed(AvailabilityStatus.InvalidServiceType, "Unknown service type code.");
        }

        var stopwatch = Stopwatch.StartNew();
        bool technicianExists;
        bool serviceBayExists;
        using (SchedulerInstrumentation.ActivitySource.StartActivity("Technician validation"))
        {
            technicianExists = await _technicianProvider.ExistsAsync(technicianId, cancellationToken);
        }

        using (SchedulerInstrumentation.ActivitySource.StartActivity("ServiceBay validation"))
        {
            serviceBayExists = await _serviceBayProvider.ExistsAsync(serviceBayId, cancellationToken);
        }

        stopwatch.Stop();
        SchedulerInstrumentation.ExternalValidationDuration.Record(stopwatch.Elapsed.TotalMilliseconds);

        if (!technicianExists || !serviceBayExists)
        {
            return AvailabilityCheckOutcome.Failed(AvailabilityStatus.InvalidResource, "Technician or Service Bay not found.");
        }

        var dealership = await _dealershipProvider.GetAsync(dealershipId, cancellationToken);
        if (dealership is null)
        {
            return AvailabilityCheckOutcome.Failed(AvailabilityStatus.InvalidResource, "Dealership not found.");
        }

        var range = new TimeRange(startTime, startTime.Add(serviceType.Duration));

        if (!dealership.IsWithinOperatingHours(range))
        {
            return AvailabilityCheckOutcome.Failed(
                AvailabilityStatus.OutsideOperatingHours,
                "Requested time is outside dealership operating hours.");
        }

        IReadOnlyList<Appointment> overlapping;
        var availabilityStopwatch = Stopwatch.StartNew();
        using (SchedulerInstrumentation.ActivitySource.StartActivity("Availability check"))
        {
            overlapping = await _appointments.GetOverlappingAsync(technicianId, serviceBayId, range, cancellationToken);
        }

        availabilityStopwatch.Stop();
        SchedulerInstrumentation.AvailabilityCheckDuration.Record(availabilityStopwatch.Elapsed.TotalMilliseconds);

        if (!AppointmentSchedulingPolicy.HasNoOverlap(overlapping, range))
        {
            return AvailabilityCheckOutcome.Failed(
                AvailabilityStatus.Unavailable,
                "Technician or Service Bay is already booked for the requested time.");
        }

        return AvailabilityCheckOutcome.Success(serviceType, range);
    }
}

public sealed class AvailabilityCheckOutcome
{
    public AvailabilityStatus Status { get; }
    public string? Reason { get; }
    public ServiceType? ServiceType { get; }
    public TimeRange? Range { get; }

    private AvailabilityCheckOutcome(AvailabilityStatus status, string? reason, ServiceType? serviceType, TimeRange? range)
    {
        Status = status;
        Reason = reason;
        ServiceType = serviceType;
        Range = range;
    }

    public static AvailabilityCheckOutcome Success(ServiceType serviceType, TimeRange range) =>
        new(AvailabilityStatus.Available, null, serviceType, range);

    public static AvailabilityCheckOutcome Failed(AvailabilityStatus status, string reason) =>
        new(status, reason, null, null);
}
