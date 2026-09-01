using Scheduler.Domain.Entities;
using Scheduler.Domain.Services;
using Scheduler.Domain.ValueObjects;

namespace Scheduler.UnitTests.Domain;

public class AppointmentSchedulingPolicyTests
{
    private static readonly Dealership Dealership = new(
        Guid.NewGuid(), "Test Dealership", new TimeOnly(8, 0), new TimeOnly(17, 0));

    // 2026-09-07 is a Monday.
    private static DateTime Monday(int hour, int minute) => new(2026, 9, 7, hour, minute, 0);

    [Fact]
    public void IsWithinOperatingHours_WithinHours_ReturnsTrue()
    {
        var range = new TimeRange(Monday(9, 0), Monday(10, 0));
        Assert.True(AppointmentSchedulingPolicy.IsWithinOperatingHours(range, Dealership));
    }

    [Fact]
    public void IsWithinOperatingHours_ExactlyAtOpen_ReturnsTrue()
    {
        var range = new TimeRange(Monday(8, 0), Monday(8, 30));
        Assert.True(AppointmentSchedulingPolicy.IsWithinOperatingHours(range, Dealership));
    }

    [Fact]
    public void IsWithinOperatingHours_ExactlyAtClose_ReturnsTrue()
    {
        var range = new TimeRange(Monday(16, 30), Monday(17, 0));
        Assert.True(AppointmentSchedulingPolicy.IsWithinOperatingHours(range, Dealership));
    }

    [Fact]
    public void IsWithinOperatingHours_BeforeOpen_ReturnsFalse()
    {
        var range = new TimeRange(Monday(7, 0), Monday(8, 0));
        Assert.False(AppointmentSchedulingPolicy.IsWithinOperatingHours(range, Dealership));
    }

    [Fact]
    public void IsWithinOperatingHours_AfterClose_ReturnsFalse()
    {
        var range = new TimeRange(Monday(16, 30), Monday(17, 30));
        Assert.False(AppointmentSchedulingPolicy.IsWithinOperatingHours(range, Dealership));
    }

    [Fact]
    public void IsWithinOperatingHours_OnSunday_ReturnsFalse()
    {
        // 2026-09-06 is a Sunday.
        var sunday = new DateTime(2026, 9, 6, 10, 0, 0);
        var range = new TimeRange(sunday, sunday.AddHours(1));
        Assert.False(AppointmentSchedulingPolicy.IsWithinOperatingHours(range, Dealership));
    }

    [Fact]
    public void IsWithinOperatingHours_CrossesMidnight_ReturnsFalse()
    {
        var range = new TimeRange(Monday(23, 0), Monday(23, 0).AddHours(2));
        Assert.False(AppointmentSchedulingPolicy.IsWithinOperatingHours(range, Dealership));
    }

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
