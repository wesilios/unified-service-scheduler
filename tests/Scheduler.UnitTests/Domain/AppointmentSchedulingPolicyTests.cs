using Scheduler.Domain.Entities;
using Scheduler.Domain.Services;
using Scheduler.Domain.ValueObjects;

namespace Scheduler.UnitTests.Domain;

public class AppointmentSchedulingPolicyTests
{
    // 2026-09-07 is a Monday.
    private static DateTime Monday(int hour, int minute) => new(2026, 9, 7, hour, minute, 0);

    [Fact]
    public void HasNoOverlap_EmptyExisting_ReturnsTrue()
    {
        var requested = new TimeRange(Monday(10, 0), Monday(11, 0));
        Assert.True(AppointmentSchedulingPolicy.HasNoOverlap(Array.Empty<Appointment>(), requested));
    }

    [Fact]
    public void HasNoOverlap_NonOverlappingExisting_ReturnsTrue()
    {
        var existing = Appointment.Create(
            "Juan Dela Cruz", "juan@example.com", "+639171234567",
            Guid.NewGuid(), "Toyota - Vios - Vios G 2019", "OIL_CHANGE",
            Guid.NewGuid(), Guid.NewGuid(), new TimeRange(Monday(8, 0), Monday(9, 0)));

        var requested = new TimeRange(Monday(10, 0), Monday(11, 0));

        Assert.True(AppointmentSchedulingPolicy.HasNoOverlap([existing], requested));
    }

    [Fact]
    public void HasNoOverlap_OverlappingExisting_ReturnsFalse()
    {
        var existing = Appointment.Create(
            "Juan Dela Cruz", "juan@example.com", "+639171234567",
            Guid.NewGuid(), "Toyota - Vios - Vios G 2019", "OIL_CHANGE",
            Guid.NewGuid(), Guid.NewGuid(), new TimeRange(Monday(9, 30), Monday(10, 30)));

        var requested = new TimeRange(Monday(10, 0), Monday(11, 0));

        Assert.False(AppointmentSchedulingPolicy.HasNoOverlap([existing], requested));
    }
}
