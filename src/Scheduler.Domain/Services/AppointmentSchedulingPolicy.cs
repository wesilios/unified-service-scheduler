using Scheduler.Domain.Entities;
using Scheduler.Domain.ValueObjects;

namespace Scheduler.Domain.Services;

// Pure, stateless domain logic — no external dependencies, so no interface/DI needed.
// IsWithinOperatingHours moved to Dealership.IsWithinOperatingHours — operating hours is
// Dealership's own invariant now that it's an internal-service-owned type, not local data.
public static class AppointmentSchedulingPolicy
{
    public static bool HasNoOverlap(IReadOnlyList<Appointment> existing, TimeRange requested)
    {
        return existing.All(appointment => !appointment.Duration.Overlaps(requested));
    }
}
