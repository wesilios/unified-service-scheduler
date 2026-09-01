namespace Scheduler.Application.Commands;

// No login required to book — Name/Email/Phone are embedded directly onto the Appointment
// as an owned Customer value object (see Data Model), not resolved against a shared record.
// If login is added later, a User would carry its own Name/Email/Phone; "my appointment
// history" becomes a query filtering Appointment by embedded Email, not a CustomerId join.
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
