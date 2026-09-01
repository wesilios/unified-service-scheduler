using Scheduler.Application.Commands;
using Scheduler.Application.Interfaces;
using Scheduler.Application.Observability;
using Scheduler.Application.Queries;
using Scheduler.Application.Results;
using Scheduler.Domain.Entities;
using Scheduler.Domain.Exceptions;
using Scheduler.Domain.Repositories;

namespace Scheduler.Application.Handlers;

public sealed class CreateAppointmentCommandHandler : ICommandHandler<CreateAppointmentCommand>
{
    private readonly IAppointmentAvailabilityChecker _availabilityChecker;
    private readonly IAppointmentRepository _appointments;
    private readonly INotificationService _notificationService;
    private readonly IAvailabilityCache _availabilityCache;

    public CreateAppointmentCommandHandler(
        IAppointmentAvailabilityChecker availabilityChecker,
        IAppointmentRepository appointments,
        INotificationService notificationService,
        IAvailabilityCache availabilityCache)
    {
        _availabilityChecker = availabilityChecker;
        _appointments = appointments;
        _notificationService = notificationService;
        _availabilityCache = availabilityCache;
    }

    public async Task<object> HandleAsync(CreateAppointmentCommand command)
    {
        using var activity = SchedulerInstrumentation.ActivitySource.StartActivity("CreateAppointment");

        var outcome = await _availabilityChecker.CheckAsync(
            command.DealershipId,
            command.TechnicianId,
            command.ServiceBayId,
            command.ServiceTypeCode,
            command.StartTime);

        if (outcome.Status != AvailabilityStatus.Available)
        {
            var failureStatus = MapToResultStatus(outcome.Status);
            RecordOutcome(failureStatus);
            return AppointmentResult.Failed(failureStatus, outcome.Reason!);
        }

        var appointment = Appointment.Create(
            command.CustomerName,
            command.CustomerEmail,
            command.CustomerPhone,
            command.DealershipId,
            command.Vehicle,
            command.ServiceTypeCode,
            command.TechnicianId,
            command.ServiceBayId,
            outcome.Range!);

        try
        {
            using (SchedulerInstrumentation.ActivitySource.StartActivity("Insert"))
            {
                await _appointments.AddAsync(appointment);
            }
        }
        catch (AppointmentConflictException)
        {
            // Lost the race to a concurrent booking for the same resource/slot — the
            // read-check above is a fast-fail optimization only; this is the real guarantee.
            RecordOutcome(AppointmentResultStatus.Conflict);
            return AppointmentResult.Failed(
                AppointmentResultStatus.Conflict,
                "Technician or Service Bay is already booked for the requested time.");
        }

        await _availabilityCache.InvalidateAsync(command.TechnicianId, command.ServiceBayId);

        using (SchedulerInstrumentation.ActivitySource.StartActivity("Notify"))
        {
            await _notificationService.SendConfirmationAsync(appointment);
        }

        RecordOutcome(AppointmentResultStatus.Success);
        return AppointmentResult.Success(appointment);
    }

    private static AppointmentResultStatus MapToResultStatus(AvailabilityStatus status) => status switch
    {
        AvailabilityStatus.InvalidServiceType => AppointmentResultStatus.InvalidServiceType,
        AvailabilityStatus.InvalidResource => AppointmentResultStatus.InvalidResource,
        AvailabilityStatus.OutsideOperatingHours => AppointmentResultStatus.OutsideOperatingHours,
        AvailabilityStatus.Unavailable => AppointmentResultStatus.Conflict,
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
    };

    private static void RecordOutcome(AppointmentResultStatus status)
    {
        SchedulerInstrumentation.BookingOutcomes.Add(1, new KeyValuePair<string, object?>("status", status.ToString()));
        if (status == AppointmentResultStatus.Conflict)
        {
            SchedulerInstrumentation.BookingConflicts.Add(1);
        }
    }
}
