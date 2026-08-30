namespace Scheduler.Application.Commands;

// No login required to book — the customer is identified by Email+Phone and either
// matched to an existing record or created as a new one (see Data Model /
// CreateAppointmentCommandHandler). If login is added later, this maps naturally onto a
// User entity: Customer becomes the booking-identity record a User account links to.
public sealed record CreateAppointmentCommand(
    string CustomerName,
    string CustomerEmail,
    string CustomerPhone,
    string Vehicle,
    string ServiceTypeCode,
    Guid DealershipId,
    Guid TechnicianId,
    Guid ServiceBayId,
    DateTime StartTime) : ICommand;
