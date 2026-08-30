using Scheduler.Application.Commands;
using Scheduler.Application.Interfaces;
using Scheduler.Application.Observability;
using Scheduler.Application.Queries;
using Scheduler.Application.Results;
using Scheduler.Application.Services;
using Scheduler.Domain.Entities;
using Scheduler.Domain.Exceptions;

namespace Scheduler.Application.Handlers;

public sealed class CreateAppointmentCommandHandler : ICommandHandler<CreateAppointmentCommand>
{
    private readonly AppointmentAvailabilityChecker _availabilityChecker;
    private readonly IAppointmentRepository _appointments;
    private readonly ICustomerRepository _customers;
    private readonly INotificationService _notificationService;
    private readonly IAvailabilityCache _availabilityCache;

    public CreateAppointmentCommandHandler(
        AppointmentAvailabilityChecker availabilityChecker,
        IAppointmentRepository appointments,
        ICustomerRepository customers,
        INotificationService notificationService,
        IAvailabilityCache availabilityCache)
    {
        _availabilityChecker = availabilityChecker;
        _appointments = appointments;
        _customers = customers;
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

        var customer = await ResolveCustomerAsync(command.CustomerName, command.CustomerEmail, command.CustomerPhone);

        var appointment = Appointment.Create(
            customer.Id,
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

    private async Task<Customer> ResolveCustomerAsync(string name, string email, string phone)
    {
        using var activity = SchedulerInstrumentation.ActivitySource.StartActivity("Resolve customer");

        var existing = await _customers.FindByEmailAndPhoneAsync(email, phone);
        if (existing is not null)
        {
            return existing;
        }

        var customer = new Customer(Guid.NewGuid(), name, email, phone);

        try
        {
            await _customers.AddAsync(customer);
            return customer;
        }
        catch (CustomerConflictException)
        {
            // Two concurrent guest-checkout requests from the same (new) customer — the
            // Email+Phone unique constraint caught it; the record now provably exists.
            return await _customers.FindByEmailAndPhoneAsync(email, phone)
                ?? throw new InvalidOperationException("Customer creation conflict could not be resolved.");
        }
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
