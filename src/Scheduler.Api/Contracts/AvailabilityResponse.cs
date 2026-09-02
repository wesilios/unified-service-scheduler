namespace Scheduler.Api.Contracts;

// Presentation-layer response DTO for GET /appointments/availability, replacing the
// controller's former anonymous object so it has a name and a schema the OpenAPI generator
// can document (an anonymous type can't be referenced from a [ProducesResponseType]
// attribute).
public sealed record AvailabilityResponse(bool Available, string Status, string? Reason);
