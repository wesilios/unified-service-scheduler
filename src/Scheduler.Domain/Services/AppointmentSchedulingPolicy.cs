using Scheduler.Domain.Entities;
using Scheduler.Domain.ValueObjects;

namespace Scheduler.Domain.Services;

// Pure, stateless domain logic — no external dependencies, so no interface/DI needed.
public static class AppointmentSchedulingPolicy
{
    public static bool IsWithinOperatingHours(TimeRange range, Dealership dealership)
    {
        if (range.Start.DayOfWeek == DayOfWeek.Sunday || range.End.DayOfWeek == DayOfWeek.Sunday)
        {
            return false;
        }

        if (range.Start.Date != range.End.Date)
        {
            return false;
        }

        var startTime = TimeOnly.FromDateTime(range.Start);
        var endTime = TimeOnly.FromDateTime(range.End);

        return startTime >= dealership.OperatingHoursStart && endTime <= dealership.OperatingHoursEnd;
    }

    public static bool HasNoOverlap(IReadOnlyList<Appointment> existing, TimeRange requested)
    {
        return existing.All(appointment => !appointment.Duration.Overlaps(requested));
    }
}
