using Scheduler.Domain.ValueObjects;

namespace Scheduler.Domain.Entities;

// One row per 15-minute increment of an Appointment's duration, per resource
// (Technician and ServiceBay). The composite UNIQUE(ResourceKind, ResourceId, SlotStart)
// index (configured in Infrastructure) is what actually prevents double-booking under
// concurrency — see architecture.md Data Model / Data Flow.
public class AppointmentSlot
{
    public static readonly TimeSpan SlotGranularity = TimeSpan.FromMinutes(15);

    public Guid Id { get; private set; }
    public Guid AppointmentId { get; private set; }
    public ResourceKind ResourceKind { get; private set; }
    public Guid ResourceId { get; private set; }
    public DateTime SlotStart { get; private set; }

    private AppointmentSlot()
    {
    }

    private AppointmentSlot(Guid appointmentId, ResourceKind resourceKind, Guid resourceId, DateTime slotStart)
    {
        Id = Guid.NewGuid();
        AppointmentId = appointmentId;
        ResourceKind = resourceKind;
        ResourceId = resourceId;
        SlotStart = slotStart;
    }

    internal static IEnumerable<AppointmentSlot> ForAppointment(
        Guid appointmentId,
        Guid technicianId,
        Guid serviceBayId,
        TimeRange range)
    {
        var totalMinutes = (range.End - range.Start).TotalMinutes;
        var slotCount = (int)Math.Ceiling(totalMinutes / SlotGranularity.TotalMinutes);

        for (var i = 0; i < slotCount; i++)
        {
            var slotStart = range.Start.Add(i * SlotGranularity);
            yield return new AppointmentSlot(appointmentId, ResourceKind.Technician, technicianId, slotStart);
            yield return new AppointmentSlot(appointmentId, ResourceKind.ServiceBay, serviceBayId, slotStart);
        }
    }
}
