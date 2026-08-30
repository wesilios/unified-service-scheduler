using Scheduler.Application.Queries;

namespace Scheduler.Api.Contracts;

// Presentation-layer request DTO, bound from the query string as a single complex type
// instead of five individual [FromQuery] parameters. Kept separate from
// CheckAvailabilityQuery for the same reason as CreateAppointmentRequest — the
// Application-layer Query type never appears in the OpenAPI/Scalar documentation.
public sealed record CheckAvailabilityRequest(
    Guid DealershipId,
    Guid TechnicianId,
    Guid ServiceBayId,
    string ServiceTypeCode,
    DateTime StartTime)
{
    public CheckAvailabilityQuery ToQuery() => new(
        DealershipId,
        TechnicianId,
        ServiceBayId,
        ServiceTypeCode,
        StartTime);
}
