namespace Scheduler.Application.Interfaces;

public interface ITechnicianService
{
    Task<bool> ExistsAsync(Guid technicianId, CancellationToken cancellationToken = default);
}
