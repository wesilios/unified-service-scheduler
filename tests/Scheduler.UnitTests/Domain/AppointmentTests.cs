using Scheduler.Domain.Entities;
using Scheduler.Domain.ValueObjects;

namespace Scheduler.UnitTests.Domain;

public class AppointmentTests
{
    private static readonly TimeRange ValidRange = new(
        new DateTime(2026, 9, 7, 10, 0, 0), new DateTime(2026, 9, 7, 10, 30, 0));

    [Fact]
    public void Create_ValidInput_PopulatesFields()
    {
        var customerId = Guid.NewGuid();
        var dealershipId = Guid.NewGuid();
        var technicianId = Guid.NewGuid();
        var serviceBayId = Guid.NewGuid();

        var appointment = Appointment.Create(
            customerId, dealershipId, "Toyota - Vios - Vios G 2019", "OIL_CHANGE",
            technicianId, serviceBayId, ValidRange);

        Assert.NotEqual(Guid.Empty, appointment.Id);
        Assert.Equal(customerId, appointment.CustomerId);
        Assert.Equal(dealershipId, appointment.DealershipId);
        Assert.Equal("Toyota - Vios - Vios G 2019", appointment.Vehicle);
        Assert.Equal("OIL_CHANGE", appointment.ServiceTypeCode);
        Assert.Equal(technicianId, appointment.TechnicianId);
        Assert.Equal(serviceBayId, appointment.ServiceBayId);
        Assert.Equal(ValidRange, appointment.Duration);
        Assert.Equal(AppointmentStatus.Confirmed, appointment.Status);
    }

    [Fact]
    public void Create_EmptyVehicle_Throws()
    {
        Assert.Throws<ArgumentException>(() => Appointment.Create(
            Guid.NewGuid(), Guid.NewGuid(), "", "OIL_CHANGE",
            Guid.NewGuid(), Guid.NewGuid(), ValidRange));
    }

    [Fact]
    public void Create_WhitespaceVehicle_Throws()
    {
        Assert.Throws<ArgumentException>(() => Appointment.Create(
            Guid.NewGuid(), Guid.NewGuid(), "   ", "OIL_CHANGE",
            Guid.NewGuid(), Guid.NewGuid(), ValidRange));
    }

    [Fact]
    public void Create_EmptyServiceTypeCode_Throws()
    {
        Assert.Throws<ArgumentException>(() => Appointment.Create(
            Guid.NewGuid(), Guid.NewGuid(), "Toyota - Vios - Vios G 2019", "",
            Guid.NewGuid(), Guid.NewGuid(), ValidRange));
    }

    [Theory]
    [InlineData(30, 4)] // 30 min -> ceil(30/15)=2 slots per resource * 2 resources = 4
    [InlineData(60, 8)] // 60 min -> 4 slots * 2 = 8
    [InlineData(45, 6)] // 45 min -> ceil(45/15)=3 slots * 2 = 6
    public void Create_GeneratesCorrectSlotCount(int durationMinutes, int expectedSlotCount)
    {
        var start = new DateTime(2026, 9, 7, 10, 0, 0);
        var range = new TimeRange(start, start.AddMinutes(durationMinutes));
        var technicianId = Guid.NewGuid();
        var serviceBayId = Guid.NewGuid();

        var appointment = Appointment.Create(
            Guid.NewGuid(), Guid.NewGuid(), "Toyota - Vios - Vios G 2019", "OIL_CHANGE",
            technicianId, serviceBayId, range);

        Assert.Equal(expectedSlotCount, appointment.Slots.Count);
        Assert.Equal(expectedSlotCount / 2, appointment.Slots.Count(s => s.ResourceKind == ResourceKind.Technician));
        Assert.Equal(expectedSlotCount / 2, appointment.Slots.Count(s => s.ResourceKind == ResourceKind.ServiceBay));
        Assert.All(
            appointment.Slots.Where(s => s.ResourceKind == ResourceKind.Technician),
            s => Assert.Equal(technicianId, s.ResourceId));
        Assert.All(
            appointment.Slots.Where(s => s.ResourceKind == ResourceKind.ServiceBay),
            s => Assert.Equal(serviceBayId, s.ResourceId));
    }
}
