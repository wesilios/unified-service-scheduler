namespace Scheduler.Domain.Exceptions;

// Thrown by IAppointmentRepository.AddAsync when the AppointmentSlot unique constraint
// rejects the insert — i.e. a concurrent request won the race for the same
// Technician/ServiceBay/time-slot. See architecture.md Data Flow for the full rationale.
public sealed class AppointmentConflictException : Exception
{
    public AppointmentConflictException(string message) : base(message)
    {
    }
}
