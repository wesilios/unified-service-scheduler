namespace Scheduler.Application.Queries;

public sealed record CheckAvailabilityQuery(
    Guid DealershipId,
    Guid TechnicianId,
    Guid ServiceBayId,
    string ServiceTypeCode,
    DateTime StartTime) : IQuery<AvailabilityResult>;

public enum AvailabilityStatus
{
    Available,
    InvalidServiceType,
    InvalidResource,
    OutsideOperatingHours,
    Unavailable
}

public sealed record AvailabilityResult(AvailabilityStatus Status, string? Reason);
