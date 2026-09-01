using Scheduler.Domain.ValueObjects;

namespace Scheduler.Domain.Entities;

public class Appointment : IAggregateRoot
{
    public Guid Id { get; private set; }
    public Customer Customer { get; private set; } = null!;
    public Guid DealershipId { get; private set; }
    public string Vehicle { get; private set; } = string.Empty;
    public string ServiceTypeCode { get; private set; } = string.Empty;
    public Guid TechnicianId { get; private set; }
    public Guid ServiceBayId { get; private set; }
    public TimeRange Duration { get; private set; } = null!;
    public AppointmentStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private readonly List<AppointmentSlot> _slots = new();
    public IReadOnlyCollection<AppointmentSlot> Slots => _slots.AsReadOnly();

    private Appointment()
    {
    }

    public static Appointment Create(
        string customerName,
        string customerEmail,
        string customerPhone,
        Guid dealershipId,
        string vehicle,
        string serviceTypeCode,
        Guid technicianId,
        Guid serviceBayId,
        TimeRange duration)
    {
        if (string.IsNullOrWhiteSpace(vehicle))
        {
            throw new ArgumentException("Vehicle description must not be empty.", nameof(vehicle));
        }

        if (string.IsNullOrWhiteSpace(serviceTypeCode))
        {
            throw new ArgumentException("ServiceTypeCode must not be empty.", nameof(serviceTypeCode));
        }

        var appointment = new Appointment
        {
            Id = Guid.NewGuid(),
            Customer = new Customer(customerName, customerEmail, customerPhone),
            DealershipId = dealershipId,
            Vehicle = vehicle,
            ServiceTypeCode = serviceTypeCode,
            TechnicianId = technicianId,
            ServiceBayId = serviceBayId,
            Duration = duration,
            Status = AppointmentStatus.Confirmed,
            CreatedAt = DateTime.UtcNow
        };

        appointment._slots.AddRange(
            AppointmentSlot.ForAppointment(appointment.Id, technicianId, serviceBayId, duration));

        return appointment;
    }
}
