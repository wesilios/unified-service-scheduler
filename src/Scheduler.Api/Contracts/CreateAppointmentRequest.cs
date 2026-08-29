using Scheduler.Application.Commands;

namespace Scheduler.Api.Contracts;

// Presentation-layer request DTO. Kept separate from CreateAppointmentCommand so the
// Application-layer Command type is never bound directly to the wire and never shows up
// in the generated OpenAPI/Scalar documentation — only this Request shape does.
public sealed record CreateAppointmentRequest(
    string CustomerName,
    string CustomerEmail,
    string CustomerPhone,
    string Vehicle,
    string ServiceTypeCode,
    Guid DealershipId,
    Guid TechnicianId,
    Guid ServiceBayId,
    DateTime StartTime)
{
    public CreateAppointmentCommand ToCommand() => new(
        CustomerName,
        CustomerEmail,
        CustomerPhone,
        Vehicle,
        ServiceTypeCode,
        DealershipId,
        TechnicianId,
        ServiceBayId,
        StartTime);
}
