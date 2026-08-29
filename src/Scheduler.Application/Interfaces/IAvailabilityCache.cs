namespace Scheduler.Application.Interfaces;

public interface IAvailabilityCache
{
    Task InvalidateAsync(Guid technicianId, Guid serviceBayId, CancellationToken cancellationToken = default);
}
