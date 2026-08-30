using Scheduler.Domain.Entities;

namespace Scheduler.Application.Results;

public enum AppointmentResultStatus
{
    Success,
    InvalidServiceType,
    InvalidResource,
    OutsideOperatingHours,
    Conflict
}

public sealed class AppointmentResult
{
    public AppointmentResultStatus Status { get; }
    public string? Error { get; }
    public Appointment? Appointment { get; }

    private AppointmentResult(AppointmentResultStatus status, Appointment? appointment, string? error)
    {
        Status = status;
        Appointment = appointment;
        Error = error;
    }

    public static AppointmentResult Success(Appointment appointment) =>
        new(AppointmentResultStatus.Success, appointment, null);

    public static AppointmentResult Failed(AppointmentResultStatus status, string error) =>
        new(status, null, error);
}
