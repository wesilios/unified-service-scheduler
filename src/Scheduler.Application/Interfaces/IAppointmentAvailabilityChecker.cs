using Scheduler.Application.Services;

namespace Scheduler.Application.Interfaces;

public interface IAppointmentAvailabilityChecker
{
    Task<AvailabilityCheckOutcome> CheckAsync(
        Guid dealershipId,
        Guid technicianId,
        Guid serviceBayId,
        string serviceTypeCode,
        DateTime startTime,
        CancellationToken cancellationToken = default);
}
