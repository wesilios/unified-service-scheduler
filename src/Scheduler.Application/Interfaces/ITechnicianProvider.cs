namespace Scheduler.Application.Interfaces;

public interface ITechnicianProvider
{
    Task<bool> ExistsAsync(Guid technicianId, CancellationToken cancellationToken = default);
}
